using LearnHub.Data.Entities;

namespace LearnHub.Repositories.Interfaces;

public interface IStudentProfileRepository
{
    Task<StudentProfile?> GetByUserIdAsync(Guid userId);
    Task AddAsync(StudentProfile profile);
    Task DeleteAsync(StudentProfile profile);
    Task<bool> ExistsAsync(Guid userId);
}
