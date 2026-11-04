using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Services.Students.DTOs;
using LearnHub.Services.Students.Interfaces;
using LearnHub.Services.Users.Interfaces;

namespace LearnHub.Services.Students;

public class StudentsService(IUsersService usersService) : IStudentsService
{
    public async Task<List<StudentResponseDto>> GetAllAsync()
    {
        var students = await usersService.GetAllAsync(UserRole.STUDENT);

        return students.Select(MapToDto).ToList();
    }

    public async Task<StudentResponseDto> GetStudentByIdAsync(Guid userId)
    {
        var student = await GetStudentOrThrow(userId);

        return MapToDto(student);
    }

    public async Task<User> UpdateRoleAsync(Guid adminId, Guid userId, ChangeRoleDto dto)
    {
        await usersService.UpdateRoleAsync(adminId, userId, dto.Role);

        return await usersService.GetUserByIdAsync(userId);
    }

    public async Task<User> SetActiveStatus(Guid userId, bool isActive)
    {
        await GetStudentOrThrow(userId);
        var user = await usersService.SetActiveStatusAsync(userId, isActive);

        return user;
    }

    private static StudentResponseDto MapToDto(User user)
    {
        return new StudentResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };
    }

    private async Task<User> GetStudentOrThrow(Guid id)
    {
        var student = await usersService.GetUserByIdAsync(id, UserRole.STUDENT);

        if (student == null || student.Role != UserRole.STUDENT)
            throw new KeyNotFoundException("Student not found.");

        return student;
    }
}
