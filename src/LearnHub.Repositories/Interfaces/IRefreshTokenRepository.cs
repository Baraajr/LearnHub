using LearnHub.Data.Entities;

namespace LearnHub.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    IQueryable<RefreshToken> GetActiveByUserId(Guid userId);

    void Update(RefreshToken refreshToken);

    Task RevokeAllByUserIdAsync(Guid userId);
}
