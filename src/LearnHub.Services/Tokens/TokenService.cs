using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnHub.Data.Entities;
using LearnHub.Repositories.RefreshTokens;
using LearnHub.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LearnHub.Services.Tokens;

public class TokenService(
    IOptions<JwtOptions> options,
    IRefreshTokenRepository refreshTokenRepository
) : ITokenService, IScopedService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new InvalidOperationException("UserName is missing.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("tokenVersion", user.TokenVersion.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    public Task CreateRefreshTokenAsync(RefreshToken refreshToken)
    {
        return refreshTokenRepository.AddAsync(refreshToken);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash)
    {
        return refreshTokenRepository
            .GetAllAsync()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(refreshToken);
    }

    public async Task<bool> TryRevokeRefreshTokenAsync(Guid tokenId)
    {
        var refreshToken = await refreshTokenRepository.GetByIdAsync(tokenId);

        if (
            refreshToken is null
            || refreshToken.RevokedAt is not null
            || refreshToken.ExpiresAt <= DateTime.UtcNow
        )
            return false;

        refreshToken.RevokedAt = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(refreshToken);

        return true;
    }

    public async Task RevokeAllRefreshTokensAsync(Guid userId)
    {
        var activeTokens = await refreshTokenRepository
            .GetAllAsync()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            await refreshTokenRepository.UpdateAsync(token);
        }
    }
}
