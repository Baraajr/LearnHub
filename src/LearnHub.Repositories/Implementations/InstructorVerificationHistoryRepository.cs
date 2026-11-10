using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.Implementations;

public class InstructorVerificationHistoryRepository(LearnHubDbContext context)
    : IInstructorVerificationHistoryRepository
{
    public async Task AddAsync(InstructorVerificationHistory history)
    {
        await context.InstructorVerificationHistories.AddAsync(history);
    }

    public async Task<List<InstructorVerificationHistory>> GetByInstructorIdAsync(
        Guid instructorId
    ) =>
        await context
            .InstructorVerificationHistories.AsNoTracking()
            .Where(h => h.InstructorId == instructorId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
}
