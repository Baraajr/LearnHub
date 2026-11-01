using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.Implementations;

public class UserRepository(LearnHubDbContext context) : IUserRepository
{
    public async Task AddAsync(User user)
    {
        await context.Users.AddAsync(user);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await context.Users.AnyAsync(u => u.Email == email);
    }

    public IQueryable<User> GetAll(UserRole? role = null)
    {
        var query = context.Users.AsNoTracking();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return query;
    }

    public async Task<int> CountAsync(UserRole? role = null)
    {
        var query = context.Users.AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query.CountAsync();
    }

    public void Update(User user)
    {
        context.Users.Update(user);
    }
}
