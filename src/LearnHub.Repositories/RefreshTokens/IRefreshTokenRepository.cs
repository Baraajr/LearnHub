using LearnHub.Data.Entities;

namespace LearnHub.Repositories.RefreshTokens;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);

    Task<RefreshToken?> GetByIdAsync(Guid id);

    IQueryable<RefreshToken> GetAllAsync();

    Task UpdateAsync(RefreshToken refreshToken);

    Task DeleteAsync(RefreshToken refreshToken);
}
