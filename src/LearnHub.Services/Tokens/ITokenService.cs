using LearnHub.Data.Entities;

namespace LearnHub.Services.Tokens;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);

    Task CreateRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> TryRevokeRefreshTokenAsync(Guid tokenId);
    Task RevokeAllRefreshTokensAsync(Guid userId);
}
