using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Auth.DTOs;
using LearnHub.Services.Auth.Interfaces;
using LearnHub.Services.Exceptions;
using LearnHub.Services.Instructors.Interfaces;
using LearnHub.Services.Options;
using LearnHub.Services.Students.Interfaces;
using Microsoft.Extensions.Options;

namespace LearnHub.Services.Auth;

public class AuthService(
    ITokenService tokenService,
    IUserRepository userRepository,
    IStudentsService studentsService,
    IInstructorsService instructorsService,
    IStudentProfileRepository studentRepository,
    IInstructorProfileRepository instructorRepository,
    IUnitOfWork unitOfWork,
    IOptions<JwtOptions> jwtOptions
) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto request)
    {
        if (request.Role is not (UserRole.STUDENT or UserRole.INSTRUCTOR))
            throw new BadRequestException("Only student or instructor registration is allowed.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Email already exists.");

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                Role = request.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            };

            await userRepository.AddAsync(user);
            await unitOfWork.SaveChangesAsync();

            if (request.Role == UserRole.STUDENT)
            {
                await studentRepository.AddAsync(new StudentProfile { UserId = user.Id });
            }
            else
            {
                await instructorRepository.AddAsync(
                    new InstructorProfile
                    {
                        UserId = user.Id,
                        VerificationStatus = InstructorVerificationStatus.PENDING,
                    }
                );

                await unitOfWork.SaveChangesAsync();

                // Instructor registration does not authenticate the instructor.
                return new AuthResponseDto { AccessToken = null!, RefreshToken = null! };
            }

            return await GenerateAuthResponseAsync(user);
        });
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email);

        if (
            user is null
            || !user.IsActive
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)
        )
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        await EnsureUserCanAuthenticateAsync(user);

        return await unitOfWork.ExecuteInTransactionAsync(() => GenerateAuthResponseAsync(user));
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var storedToken = await tokenService.GetRefreshTokenAsync(hash);

        if (
            storedToken is null
            || storedToken.RevokedAt is not null
            || storedToken.ExpiresAt <= DateTime.UtcNow
        )
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = await userRepository.GetByIdAsync(storedToken.UserId);

        if (user is null || !user.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        try
        {
            await EnsureUserCanAuthenticateAsync(user);
        }
        catch (ForbiddenException)
        {
            // Revoke the refresh token if the user's eligibility has been revoked.
            await tokenService.TryRevokeRefreshTokenAsync(storedToken.Id);
            await unitOfWork.SaveChangesAsync();
            throw;
        }

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var revoked = await tokenService.TryRevokeRefreshTokenAsync(storedToken.Id);

            if (!revoked)
                throw new UnauthorizedException("Invalid or expired refresh token.");

            return await GenerateAuthResponseAsync(user);
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
                throw new NotFoundException("User not found.");

            user.TokenVersion++;

            await tokenService.RevokeAllRefreshTokensAsync(userId);
            await unitOfWork.SaveChangesAsync();

            return true;
        });
    }

    private async Task EnsureUserCanAuthenticateAsync(User user)
    {
        switch (user.Role)
        {
            case UserRole.STUDENT:
                await studentsService.EnsureCanLoginAsync(user.Id);
                break;

            case UserRole.INSTRUCTOR:
                await instructorsService.EnsureCanLoginAsync(user.Id);
                break;
        }
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
    {
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
}
