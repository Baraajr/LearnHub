using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.StudentProfiles;
using LearnHub.Repositories.Users;
using LearnHub.Services.Exceptions;
using LearnHub.Services.Students.DTOs;
using LearnHub.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services.Students;

public class StudentsService(
    IUserRepository userRepo,
    IStudentProfileRepository studentRepo,
    IUsersService usersService
) : IStudentsService
{
    public async Task<List<StudentResponseDto>> GetAllAsync()
    {
        return await GetStudentQuery().ToListAsync();
    }

    public async Task<StudentResponseDto> GetStudentByIdAsync(Guid userId)
    {
        var student = await GetStudentQuery().FirstOrDefaultAsync(s => s.Id == userId);

        return student ?? throw new NotFoundException("Student not found.");
    }

    public async Task<User> UpdateRoleAsync(Guid adminId, Guid userId, ChangeRoleDto dto)
    {
        var student = await userRepo.GetByIdAsync(userId);

        if (student is null || student.Role != UserRole.STUDENT)
            throw new NotFoundException("Student not found.");

        await usersService.UpdateRoleAsync(adminId, userId, dto.Role);

        return await userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
    }

    public async Task<User> SetActiveStatus(Guid userId, bool isActive)
    {
        var student = await userRepo.GetByIdAsync(userId);

        if (student is null || student.Role != UserRole.STUDENT)
            throw new NotFoundException("Student not found.");

        return await usersService.SetActiveStatusAsync(userId, isActive);
    }

    public async Task EnsureCanLoginAsync(Guid userId)
    {
        var student = await userRepo.GetByIdAsync(userId);

        if (student is null || student.Role != UserRole.STUDENT)
            throw new NotFoundException("Student not found.");

        if (!student.IsActive)
            throw new ForbiddenException("Your student account is inactive.");
    }

    private IQueryable<StudentResponseDto> GetStudentQuery()
    {
        var users = userRepo.GetAllAsync();
        var profiles = studentRepo.GetAllAsync();

        var query =
            from profile in profiles
            join user in users on profile.UserId equals user.Id
            where user.Role == UserRole.STUDENT
            select new StudentResponseDto
            {
                // Account
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                IsActive = user.IsActive,

                // Profile audit
                CreatedAt = profile.CreatedAt,
                CreatedBy = profile.CreatedBy,
                UpdatedAt = profile.UpdatedAt,
                UpdatedBy = profile.UpdatedBy,

                // Account audit
                AccountCreatedAt = user.CreatedAt,
                AccountCreatedBy = user.CreatedBy,
                AccountUpdatedAt = user.UpdatedAt,
                AccountUpdatedBy = user.UpdatedBy,
            };

        return query;
    }
}
