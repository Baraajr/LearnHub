using LearnHub.Data.Entities;
using LearnHub.Data.Enums;

namespace LearnHub.Repositories.Interfaces;

public interface IInstructorProfileRepository
{
    Task<List<InstructorProfile>> GetAllAsync();

    Task<InstructorProfile?> GetByUserIdAsync(Guid userId);

    Task<int> CountByStatusAsync(InstructorVerificationStatus status);

    Task<IEnumerable<InstructorProfile>> GetPendingAsync(int page, int pageSize);

    Task UpdateAsync(InstructorProfile profile);
    Task AddAsync(InstructorProfile profile);
    Task DeleteAsync(InstructorProfile profile);
}
