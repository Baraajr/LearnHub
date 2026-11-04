using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.Implementations;

public class InstructorProfileRepository(LearnHubDbContext context) : IInstructorProfileRepository
{
    public async Task<List<InstructorProfile>> GetAllAsync()
    {
        return await context.InstructorProfiles.AsNoTracking().Include(i => i.User).ToListAsync();
    }

    public async Task<InstructorProfile?> GetByUserIdAsync(Guid userId)
    {
        return await context.InstructorProfiles.FirstOrDefaultAsync(i => i.UserId == userId);
    }

    public async Task<int> CountByStatusAsync(InstructorVerificationStatus status)
    {
        return await context.InstructorProfiles.CountAsync(i => i.VerificationStatus == status);
    }

    public async Task<IEnumerable<InstructorProfile>> GetPendingAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await context
            .InstructorProfiles.AsNoTracking()
            .Include(i => i.User)
            .Where(i => i.VerificationStatus == InstructorVerificationStatus.PENDING)
            .OrderBy(i => i.CreatedAt)
            .ThenBy(i => i.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task UpdateAsync(InstructorProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;

        if (context.Entry(profile).State == EntityState.Detached)
            context.InstructorProfiles.Update(profile);

        return Task.CompletedTask;
    }

    public async Task AddAsync(InstructorProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;

        await context.InstructorProfiles.AddAsync(profile);
    }

    public Task DeleteAsync(InstructorProfile profile)
    {
        context.InstructorProfiles.Remove(profile);

        return Task.CompletedTask;
    }
}
