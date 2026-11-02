using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.Implementations;

public class RefreshTokenRepository(LearnHubDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken)
    {
        await context.RefreshTokens.AddAsync(refreshToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public IQueryable<RefreshToken> GetActiveByUserId(Guid userId)
    {
        return context
            .RefreshTokens.AsNoTracking()
            .Where(rt =>
                rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow
            );
    }

    public void Update(RefreshToken refreshToken)
    {
        context.RefreshTokens.Update(refreshToken);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId)
    {
        await context
            .RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
            );
    }

    public async Task<bool> TryRevokeAsync(Guid tokenId)
    {
        var affectedRows = await context
            .RefreshTokens.Where(rt =>
                rt.Id == tokenId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow
            )
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
            );

        return affectedRows == 1;
    }
}
