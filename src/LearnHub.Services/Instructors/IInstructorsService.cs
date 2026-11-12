using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Services.Instructors.DTOs;

namespace LearnHub.Services.Instructors;

public interface IInstructorsService
{
    Task<List<InstructorResponseDto>> GetAllAsync();
    Task<InstructorResponseDto> GetInstructorByIdAsync(Guid userId);
    Task<User> UpdateRoleAsync(Guid adminId, Guid userId, UserRole newRole);
    Task UpdateStatusAsync(Guid adminId, Guid userId, UpdateInstructorStatusDto dto);
    Task<InstructorResponseDto> SetActiveAsync(Guid userId, bool isActive);
    Task<List<InstructorVerificationHistoryDto>> GetVerificationHistoryAsync(Guid instructorId);
    Task EnsureCanLoginAsync(Guid userId);
}
