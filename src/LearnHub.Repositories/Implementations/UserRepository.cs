using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.Implementations;

public class UserRepository(LearnHubDbContext context) : IUserRepository
{
    public async Task<List<User>> GetAllSync(UserRole? role = null)
    {
        var query = context.Users.AsNoTracking();

        if (role.HasValue)
            query = query.Where(u => u.Role == role);

        if (role == UserRole.INSTRUCTOR)
            query = query.Include(u => u.InstructorProfile).ThenInclude(p => p!.Reviewer);
        else if (role == UserRole.STUDENT)
            query = query.Include(u => u.StudentProfile);

        var users = await query.ToListAsync();

        return users;
    }

    public async Task<User?> GetByIdAsync(Guid id, UserRole? role = null)
    {
        var query = context.Users.AsQueryable();
        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (role == UserRole.INSTRUCTOR)
            query = query.Include(u => u.InstructorProfile);
        else if (role == UserRole.STUDENT)
            query = query.Include(u => u.StudentProfile);

        return await query.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(User user)
    {
        await context.Users.AddAsync(user);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? exceptUserId = null)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await context.Users.AnyAsync(u =>
            u.NormalizedEmail == normalizedEmail && (exceptUserId == null || u.Id != exceptUserId)
        );
    }

    public async Task<int> CountAsync(UserRole? role = null)
    {
        var query = context.Users.AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query.CountAsync();
    }

    public async Task<int> CountActiveAsync(UserRole? role = null)
    {
        var query = context.Users.Where(u => u.IsActive);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query.CountAsync();
    }

    public void Update(User user)
    {
        context.Users.Update(user);
    }

    public async Task<bool> ExistsAsync(Guid userId, UserRole? role = null)
    {
        var query = context.Users.Where(u => u.Id == userId);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query.AnyAsync();
    }
}
