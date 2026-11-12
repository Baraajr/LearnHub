using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.InstructorProfiles;

public class InstructorProfileRepository(LearnHubDbContext context) : IInstructorProfileRepository
{
    public IQueryable<InstructorProfile> GetAllAsync()
    {
        return context.InstructorProfiles.AsNoTracking();
    }

    public async Task<InstructorProfile?> GetByIdAsync(Guid id)
    {
        return await context.InstructorProfiles.FindAsync(id);
    }

    public async Task AddAsync(InstructorProfile profile)
    {
        await context.InstructorProfiles.AddAsync(profile);
    }

    public Task UpdateAsync(InstructorProfile profile)
    {
        context.InstructorProfiles.Update(profile);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(InstructorProfile profile)
    {
        context.InstructorProfiles.Remove(profile);
        return Task.CompletedTask;
    }
}
