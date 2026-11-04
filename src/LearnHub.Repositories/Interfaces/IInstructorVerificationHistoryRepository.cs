using LearnHub.Data.Entities;

namespace LearnHub.Repositories.Interfaces;

public interface IInstructorVerificationHistoryRepository
{
    Task<List<InstructorVerificationHistory>> GetByInstructorIdAsync(Guid instructorId);

    Task AddAsync(InstructorVerificationHistory history);
}
