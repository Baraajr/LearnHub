using LearnHub.Data.Entities;

namespace LearnHub.Repositories.InstructorVerificationHistories;

public interface IInstructorVerificationHistoryRepository
{
    Task AddAsync(InstructorVerificationHistory history);
    IQueryable<InstructorVerificationHistory> GetAllAsync();
}
