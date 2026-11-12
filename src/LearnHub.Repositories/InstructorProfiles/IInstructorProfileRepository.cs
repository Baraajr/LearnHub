using LearnHub.Data.Entities;

namespace LearnHub.Repositories.InstructorProfiles;

public interface IInstructorProfileRepository
{
    IQueryable<InstructorProfile> GetAllAsync();

    Task<InstructorProfile?> GetByIdAsync(Guid id);

    Task AddAsync(InstructorProfile profile);
    Task UpdateAsync(InstructorProfile profile);
    Task DeleteAsync(InstructorProfile profile);
}
