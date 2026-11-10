using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Admins.DTOs;
using LearnHub.Services.Admins.Interfaces;
using LearnHub.Services.Exceptions;
using LearnHub.Services.Users.Interfaces;

namespace LearnHub.Services.Admins;

public class AdminService(
    IUserRepository userRepository,
    IUsersService usersService,
    IUnitOfWork unitOfWork
) : IAdminService
{
    public async Task<IReadOnlyList<AdminDto>> GetAllAsync()
    {
        var admins = await usersService.GetAllAsync(UserRole.ADMIN);

        return admins.Select(MapToDto).ToList();
    }

    public async Task<AdminDto> GetByIdAsync(Guid id)
    {
        return MapToDto(await GetAdminOrThrow(id));
    }

    public async Task<AdminDto> CreateAsync(CreateAdminDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Email already exists.");

        var admin = new User
        {
            UserName = dto.UserName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.ADMIN,
            IsActive = true,
        };

        await userRepository.AddAsync(admin);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(admin);
    }

    public async Task DeleteAsync(Guid id)
    {
        var admin = await GetAdminOrThrow(id);

        if (!admin.IsActive)
            return;

        var activeAdmins = await userRepository.CountActiveAsync(UserRole.ADMIN);

        if (activeAdmins <= 1)
            throw new ConflictException("Cannot deactivate the last active administrator.");

        admin.IsActive = false;

        await unitOfWork.SaveChangesAsync();
    }

    public async Task ActivateAsync(Guid id)
    {
        var admin = await GetAdminOrThrow(id);

        if (admin.IsActive)
            return;

        admin.IsActive = true;

        await unitOfWork.SaveChangesAsync();
    }

    private async Task<User> GetAdminOrThrow(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);

        if (user is null || user.Role != UserRole.ADMIN)
            throw new NotFoundException("Administrator not found.");

        return user;
    }

    private static AdminDto MapToDto(User user)
    {
        return new AdminDto
        {
            Id = user.Id,
            UserName = user.UserName ?? throw new InvalidOperationException("UserName is missing."),
            Email = user.Email ?? throw new InvalidOperationException("Email is missing."),
            IsActive = user.IsActive,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            UpdatedAt = user.UpdatedAt,
            UpdatedBy = user?.UpdatedBy,
        };
    }
}
