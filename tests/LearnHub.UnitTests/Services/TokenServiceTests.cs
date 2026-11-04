using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Auth;
using LearnHub.Services.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace LearnHub.UnitTests.Services.Auth;

public class TokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly TokenService _sut;

    private const string Secret = "this-is-a-test-secret-key-with-at-least-32-bytes";

    public TokenServiceTests()
    {
        var options = Options.Create(
            new JwtOptions
            {
                Secret = Secret,
                Issuer = "LearnHub",
                Audience = "LearnHub",
                AccessTokenLifetimeMinutes = 15,
            }
        );

        _sut = new TokenService(options, _refreshTokenRepository.Object);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_GeneratesValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Baraa",
            Role = UserRole.STUDENT,
            TokenVersion = 2,
        };

        // Act
        var tokenString = _sut.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        // Assert
        Assert.Equal("LearnHub", token.Issuer);
        Assert.Contains("LearnHub", token.Audiences);

        Assert.Equal(
            user.Id.ToString(),
            token.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
        );

        Assert.Equal(user.Name, token.Claims.First(x => x.Type == ClaimTypes.Name).Value);

        Assert.Equal(
            user.Role.ToString(),
            token.Claims.First(x => x.Type == ClaimTypes.Role).Value
        );

        Assert.Equal("2", token.Claims.First(x => x.Type == "tokenVersion").Value);

        Assert.True(token.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesSecureRandomToken()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);

        Assert.Equal(64, Convert.FromBase64String(token1).Length);
    }

    [Fact]
    public void HashRefreshToken_ValidToken_ReturnsConsistentHash()
    {
        // Arrange
        var refreshToken = "my-refresh-token";

        // Act
        var hash1 = _sut.HashRefreshToken(refreshToken);
        var hash2 = _sut.HashRefreshToken(refreshToken);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(refreshToken, hash1);

        Assert.Equal(32, Convert.FromBase64String(hash1).Length);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ValidToken_CallsRepository()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hashed-token",
        };

        // Act
        await _sut.CreateRefreshTokenAsync(refreshToken);

        // Assert
        _refreshTokenRepository.Verify(x => x.AddAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ExistingToken_ReturnsToken()
    {
        // Arrange
        var refreshToken = new RefreshToken { Id = Guid.NewGuid(), TokenHash = "hashed-token" };

        _refreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync("hashed-token"))
            .ReturnsAsync(refreshToken);

        // Act
        var result = await _sut.GetRefreshTokenAsync("hashed-token");

        // Assert
        Assert.Same(refreshToken, result);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidToken_SetsRevokedAtAndUpdatesRepository()
    {
        // Arrange
        var refreshToken = new RefreshToken { Id = Guid.NewGuid() };

        // Act
        await _sut.RevokeRefreshTokenAsync(refreshToken);

        // Assert
        Assert.NotNull(refreshToken.RevokedAt);

        _refreshTokenRepository.Verify(x => x.Update(refreshToken), Times.Once);
    }

    [Fact]
    public async Task TryRevokeRefreshTokenAsync_ValidId_ReturnsRepositoryResult()
    {
        // Arrange
        var tokenId = Guid.NewGuid();

        _refreshTokenRepository.Setup(x => x.TryRevokeAsync(tokenId)).ReturnsAsync(true);

        // Act
        var result = await _sut.TryRevokeRefreshTokenAsync(tokenId);

        // Assert
        Assert.True(result);

        _refreshTokenRepository.Verify(x => x.TryRevokeAsync(tokenId), Times.Once);
    }

    [Fact]
    public async Task RevokeAllRefreshTokensAsync_ValidUserId_CallsRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _sut.RevokeAllRefreshTokensAsync(userId);

        // Assert
        _refreshTokenRepository.Verify(x => x.RevokeAllByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public void GenerateAccessToken_UsesHmacSha256()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Baraa",
            Role = UserRole.STUDENT,
        };

        // Act
        var tokenString = _sut.GenerateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        // Assert
        Assert.Equal("HS256", token.Header.Alg);
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredExpiration()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Baraa",
            Role = UserRole.STUDENT,
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var tokenString = _sut.GenerateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        var afterGeneration = DateTime.UtcNow;

        // Assert
        Assert.InRange(
            token.ValidTo,
            beforeGeneration.AddMinutes(15).AddSeconds(-2),
            afterGeneration.AddMinutes(15).AddSeconds(2)
        );
    }

    [Fact]
    public void HashRefreshToken_DifferentTokens_ProducesDifferentHashes()
    {
        // Act
        var hash1 = _sut.HashRefreshToken("refresh-token-1");
        var hash2 = _sut.HashRefreshToken("refresh-token-2");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateAccessToken_ValidSecret_PassesSignatureValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Baraa",
            Role = UserRole.STUDENT,
        };

        var tokenString = _sut.GenerateAccessToken(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "LearnHub",

            ValidateAudience = true,
            ValidAudience = "LearnHub",

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Act
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            tokenString,
            validationParameters,
            out var validatedToken
        );

        // Assert
        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal("Baraa", principal.Identity?.Name);
    }

    [Fact]
    public void GenerateAccessToken_WrongSecret_FailsSignatureValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Baraa",
            Role = UserRole.STUDENT,
        };

        var tokenString = _sut.GenerateAccessToken(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("a-different-secret-key-with-32-bytes")
            ),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
        };

        // Act & Assert
        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParameters, out _)
        );
    }
}
