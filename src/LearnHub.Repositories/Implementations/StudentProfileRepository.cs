using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.Implementations;

public class StudentProfileRepository(LearnHubDbContext context) : IStudentProfileRepository
{
    public async Task<StudentProfile?> GetByUserIdAsync(Guid userId)
    {
        return await context
            .StudentProfiles.Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<bool> ExistsAsync(Guid userId)
    {
        return await context.StudentProfiles.AnyAsync(s => s.UserId == userId);
    }

    public async Task AddAsync(StudentProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;
        await context.StudentProfiles.AddAsync(profile);
    }

    public Task DeleteAsync(StudentProfile profile)
    {
        context.StudentProfiles.Remove(profile);
        return Task.CompletedTask;
    }
}
