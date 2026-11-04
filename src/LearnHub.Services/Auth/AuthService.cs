using System.ComponentModel.DataAnnotations;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Auth.DTOs;
using LearnHub.Services.Auth.Interfaces;
using LearnHub.Services.Options;
using Microsoft.Extensions.Options;

namespace LearnHub.Services.Auth;

public class AuthService(
    ITokenService tokenService,
    IUserRepository userRepository,
    IStudentProfileRepository studentRepository,
    IInstructorProfileRepository instructorRepository,
    IUnitOfWork unitOfWork,
    IOptions<JwtOptions> jwtOptions
) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto request)
    {
        // Public registration is limited to STUDENT and INSTRUCTOR.
        if (request.Role is not (UserRole.STUDENT or UserRole.INSTRUCTOR))
            throw new ValidationException("Only student or instructor registration is allowed.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(email))
            return null;

        AuthResponseDto? response = null;

        // User, profile and refresh token are created atomically.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                Role = request.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            };

            await userRepository.AddAsync(user);
            await unitOfWork.SaveChangesAsync(); // makes sure user.Id is available for the FKs

            if (request.Role == UserRole.STUDENT)
            {
                await studentRepository.AddAsync(new StudentProfile { UserId = user.Id });
            }
            else // INSTRUCTOR
            {
                await instructorRepository.AddAsync(
                    new InstructorProfile
                    {
                        UserId = user.Id,
                        VerificationStatus = InstructorVerificationStatus.PENDING,
                    }
                );
            }

            var refreshToken = tokenService.GenerateRefreshToken();

            await tokenService.CreateRefreshTokenAsync(
                new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = tokenService.HashRefreshToken(refreshToken),
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
                }
            );

            await unitOfWork.SaveChangesAsync();

            response = new AuthResponseDto
            {
                AccessToken = tokenService.GenerateAccessToken(user),
                RefreshToken = refreshToken,
            };

            return true;
        });

        return response;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email);

        if (
            user is null
            || !user.IsActive
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)
        )
        {
            return null;
        }

        var refreshToken = tokenService.GenerateRefreshToken();

        await tokenService.CreateRefreshTokenAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashRefreshToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            }
        );
        await unitOfWork.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = tokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken,
        };
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var storedToken = await tokenService.GetRefreshTokenAsync(hash);

        if (
            storedToken is null
            || storedToken.RevokedAt is not null
            || storedToken.ExpiresAt <= DateTime.UtcNow
        )
            return null;

        var user = await userRepository.GetByIdAsync(storedToken.UserId);

        if (user is null || !user.IsActive)
            return null;

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var revoked = await tokenService.TryRevokeRefreshTokenAsync(storedToken.Id);

            if (!revoked)
                return null;

            var newRefreshToken = tokenService.GenerateRefreshToken();

            await tokenService.CreateRefreshTokenAsync(
                new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = tokenService.HashRefreshToken(newRefreshToken),
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays), // Dynamic
                }
            );

            await unitOfWork.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = tokenService.GenerateAccessToken(user),
                RefreshToken = newRefreshToken,
            };
        });
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var storedToken = await tokenService.GetRefreshTokenAsync(hash);

        if (storedToken is null || storedToken.RevokedAt is not null)
            return false;

        var revoked = await tokenService.TryRevokeRefreshTokenAsync(storedToken.Id);

        if (!revoked)
            return false;

        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task LogoutAllAsync(Guid userId)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = await userRepository.GetByIdAsync(userId);

            if (user is null)
                return true;

            user.TokenVersion++;
            await tokenService.RevokeAllRefreshTokensAsync(userId);
            await unitOfWork.SaveChangesAsync();

            return true;
        });
    }
}
