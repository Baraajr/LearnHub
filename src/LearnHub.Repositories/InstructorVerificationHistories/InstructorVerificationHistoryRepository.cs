using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.InstructorVerificationHistories;

public class InstructorVerificationHistoryRepository(LearnHubDbContext context)
    : IInstructorVerificationHistoryRepository
{
    public async Task AddAsync(InstructorVerificationHistory history)
    {
        await context.InstructorVerificationHistories.AddAsync(history);
    }

    public IQueryable<InstructorVerificationHistory> GetAllAsync()
    {
        return context.InstructorVerificationHistories.AsNoTracking();
    }
}
