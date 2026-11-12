using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.StudentProfiles;

public class StudentProfileRepository(LearnHubDbContext context) : IStudentProfileRepository
{
    public IQueryable<StudentProfile> GetAllAsync()
    {
        return context.StudentProfiles.AsNoTracking();
    }

    public async Task<StudentProfile?> GetByIdAsync(Guid id)
    {
        return await context.StudentProfiles.FindAsync(id);
    }

    public async Task AddAsync(StudentProfile profile)
    {
        await context.StudentProfiles.AddAsync(profile);
    }

    public Task UpdateAsync(StudentProfile profile)
    {
        context.StudentProfiles.Update(profile);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StudentProfile profile)
    {
        context.StudentProfiles.Remove(profile);
        return Task.CompletedTask;
    }
}
