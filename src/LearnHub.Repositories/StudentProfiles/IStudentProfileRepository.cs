using LearnHub.Data.Entities;

namespace LearnHub.Repositories.StudentProfiles;

public interface IStudentProfileRepository
{
    IQueryable<StudentProfile> GetAllAsync();

    Task<StudentProfile?> GetByIdAsync(Guid id);

    Task AddAsync(StudentProfile profile);
    Task UpdateAsync(StudentProfile profile);
    Task DeleteAsync(StudentProfile profile);
}
