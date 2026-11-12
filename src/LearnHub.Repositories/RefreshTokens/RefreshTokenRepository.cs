using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.RefreshTokens;

public class RefreshTokenRepository(LearnHubDbContext context)
    : IRefreshTokenRepository,
        IScopedRepository
{
    public async Task AddAsync(RefreshToken refreshToken)
    {
        await context.RefreshTokens.AddAsync(refreshToken);
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id)
    {
        return await context.RefreshTokens.FindAsync(id);
    }

    public IQueryable<RefreshToken> GetAllAsync()
    {
        return context.RefreshTokens.AsNoTracking();
    }

    public Task UpdateAsync(RefreshToken refreshToken)
    {
        context.RefreshTokens.Update(refreshToken);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RefreshToken refreshToken)
    {
        context.RefreshTokens.Remove(refreshToken);
        return Task.CompletedTask;
    }
}
