using LearnHub.Data.Entities;
using LearnHub.Services.Students.DTOs;

namespace LearnHub.Services.Students.Interfaces;

public interface IStudentsService
{
    Task<List<StudentResponseDto>> GetAllAsync();
    Task<StudentResponseDto> GetStudentByIdAsync(Guid userId);
    Task<User> UpdateRoleAsync(Guid adminId, Guid userId, ChangeRoleDto dto);
    Task<User> SetActiveStatus(Guid userId, bool isActive);
}
