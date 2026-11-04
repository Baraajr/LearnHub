using System.ComponentModel.DataAnnotations;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Repositories.Interfaces;
using LearnHub.Services.Auth;
using LearnHub.Services.Auth.DTOs;
using LearnHub.Services.Auth.Interfaces;
using LearnHub.Services.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace LearnHub.UnitTests.Services.Auth;

public class AuthServiceTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IStudentProfileRepository> _studentRepository = new();
    private readonly Mock<IInstructorProfileRepository> _instructorRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _tokenService.Object,
            _userRepository.Object,
            _studentRepository.Object,
            _instructorRepository.Object,
            _unitOfWork.Object,
            Options.Create(
                new JwtOptions
                {
                    RefreshTokenLifetimeDays = 7,
                    AccessTokenLifetimeMinutes = 15,
                    Secret = "test-secret",
                    Issuer = "LearnHub",
                    Audience = "LearnHub",
                }
            )
        );

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> operation) => operation());

        _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<AuthResponseDto?>>>()))
            .Returns((Func<Task<AuthResponseDto?>> operation) => operation());
        _tokenService.Setup(x => x.GenerateRefreshToken()).Returns("plain-refresh-token");

        _tokenService
            .Setup(x => x.HashRefreshToken("plain-refresh-token"))
            .Returns("hashed-refresh-token");

        _tokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");

        _tokenService
            .Setup(x => x.CreateRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task RegisterAsync_ValidStudent_CreatesStudentAndReturnsTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedExpiry = DateTime.UtcNow.AddDays(7);

        User? createdUser = null;
        StudentProfile? createdProfile = null;
        RefreshToken? createdToken = null;

        _userRepository.Setup(x => x.ExistsByEmailAsync("baraa@gmail.com")).ReturnsAsync(false);

        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user =>
            {
                user.Id = userId;
                createdUser = user;
            })
            .Returns(Task.CompletedTask);

        _studentRepository
            .Setup(x => x.AddAsync(It.IsAny<StudentProfile>()))
            .Callback<StudentProfile>(profile => createdProfile = profile)
            .Returns(Task.CompletedTask);

        _tokenService
            .Setup(x => x.CreateRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => createdToken = token)
            .Returns(Task.CompletedTask);

        var request = new RegisterDto
        {
            Name = " Baraa ",
            Email = " BARAA@GMAIL.COM ",
            Password = "password123",
            Role = UserRole.STUDENT,
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("plain-refresh-token", result.RefreshToken);

        Assert.NotNull(createdUser);
        Assert.Equal(userId, createdUser.Id);
        Assert.Equal("Baraa", createdUser.Name);
        Assert.Equal("baraa@gmail.com", createdUser.Email);
        Assert.Equal(UserRole.STUDENT, createdUser.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", createdUser.PasswordHash));

        Assert.NotNull(createdProfile);
        Assert.Equal(userId, createdProfile.UserId);

        Assert.NotNull(createdToken);
        Assert.Equal(userId, createdToken.UserId);
        Assert.Equal("hashed-refresh-token", createdToken.TokenHash);
        Assert.NotNull(createdToken);
        Assert.InRange(
            createdToken.ExpiresAt,
            expectedExpiry.AddSeconds(-5),
            expectedExpiry.AddSeconds(5)
        );
        _instructorRepository.Verify(x => x.AddAsync(It.IsAny<InstructorProfile>()), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task RegisterAsync_ValidInstructor_CreatesInstructorAndReturnsTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        User? createdUser = null;
        InstructorProfile? createdProfile = null;

        _userRepository
            .Setup(x => x.ExistsByEmailAsync("instructor@gmail.com"))
            .ReturnsAsync(false);

        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user =>
            {
                user.Id = userId;
                createdUser = user;
            })
            .Returns(Task.CompletedTask);

        _instructorRepository
            .Setup(x => x.AddAsync(It.IsAny<InstructorProfile>()))
            .Callback<InstructorProfile>(profile => createdProfile = profile)
            .Returns(Task.CompletedTask);

        var request = new RegisterDto
        {
            Name = "Instructor",
            Email = "instructor@gmail.com",
            Password = "password123",
            Role = UserRole.INSTRUCTOR,
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);

        Assert.NotNull(createdUser);
        Assert.Equal(UserRole.INSTRUCTOR, createdUser.Role);

        Assert.NotNull(createdProfile);
        Assert.Equal(userId, createdProfile.UserId);
        Assert.Equal(InstructorVerificationStatus.PENDING, createdProfile.VerificationStatus);

        _studentRepository.Verify(x => x.AddAsync(It.IsAny<StudentProfile>()), Times.Never);
    }

    [Theory]
    [InlineData(UserRole.ADMIN)]
    [InlineData(UserRole.SUPERADMIN)]
    public async Task RegisterAsync_RestrictedRole_ThrowsValidationException(UserRole role)
    {
        // Arrange
        var request = new RegisterDto
        {
            Name = "Admin",
            Email = "admin@gmail.com",
            Password = "password123",
            Role = role,
        };

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RegisterAsync(request)
        );

        // Assert
        Assert.Equal("Only student or instructor registration is allowed.", exception.Message);

        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsNull()
    {
        // Arrange
        _userRepository.Setup(x => x.ExistsByEmailAsync("baraa@gmail.com")).ReturnsAsync(true);

        var request = new RegisterDto
        {
            Name = "Baraa",
            Email = "BARAA@GMAIL.COM",
            Password = "password123",
            Role = UserRole.STUDENT,
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.Null(result);

        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RegisterAsync_TransactionFails_ThrowsException()
    {
        // Arrange
        _userRepository.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        var request = new RegisterDto
        {
            Name = "Baraa",
            Email = "baraa@gmail.com",
            Password = "password123",
            Role = UserRole.STUDENT,
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _sut.RegisterAsync(request));

        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Baraa",
            Email = "baraa@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.STUDENT,
            IsActive = true,
        };

        RefreshToken? createdToken = null;

        _userRepository.Setup(x => x.GetByEmailAsync("baraa@gmail.com")).ReturnsAsync(user);

        _tokenService
            .Setup(x => x.CreateRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => createdToken = token)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.LoginAsync(
            new LoginDto { Email = "baraa@gmail.com", Password = "password123" }
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("plain-refresh-token", result.RefreshToken);

        Assert.NotNull(createdToken);
        Assert.Equal(user.Id, createdToken.UserId);
        Assert.Equal("hashed-refresh-token", createdToken.TokenHash);

        _tokenService.Verify(x => x.GenerateAccessToken(user), Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        _userRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(
            new LoginDto { Email = "unknown@gmail.com", Password = "password123" }
        );

        // Assert
        Assert.Null(result);

        _tokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "baraa@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            IsActive = true,
        };

        _userRepository.Setup(x => x.GetByEmailAsync("baraa@gmail.com")).ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(
            new LoginDto { Email = "baraa@gmail.com", Password = "wrong-password" }
        );

        // Assert
        Assert.Null(result);

        _tokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "baraa@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = false,
        };

        _userRepository.Setup(x => x.GetByEmailAsync("baraa@gmail.com")).ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(
            new LoginDto { Email = "baraa@gmail.com", Password = "password123" }
        );

        // Assert
        Assert.Null(result);

        _tokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_EmailWithWhitespaceAndUppercase_NormalizesEmail()
    {
        // Arrange
        _userRepository.Setup(x => x.GetByEmailAsync("baraa@gmail.com")).ReturnsAsync((User?)null);

        // Act
        await _sut.LoginAsync(
            new LoginDto { Email = " BARAA@GMAIL.COM ", Password = "password123" }
        );

        // Assert
        _userRepository.Verify(x => x.GetByEmailAsync("baraa@gmail.com"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_SaveChangesFails_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "baraa@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true,
        };

        _userRepository.Setup(x => x.GetByEmailAsync("baraa@gmail.com")).ReturnsAsync(user);

        _unitOfWork.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _sut.LoginAsync(new LoginDto { Email = "baraa@gmail.com", Password = "password123" })
        );
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "old-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(2),
        };

        var user = new User
        {
            Id = userId,
            Name = "Baraa",
            IsActive = true,
        };

        RefreshToken? newToken = null;

        _tokenService.Setup(x => x.HashRefreshToken("old-refresh-token")).Returns("old-hash");

        _tokenService.Setup(x => x.GetRefreshTokenAsync("old-hash")).ReturnsAsync(storedToken);

        _userRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        _tokenService.Setup(x => x.TryRevokeRefreshTokenAsync(storedToken.Id)).ReturnsAsync(true);

        _tokenService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");

        _tokenService.Setup(x => x.HashRefreshToken("new-refresh-token")).Returns("new-hash");

        _tokenService
            .Setup(x => x.CreateRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => newToken = token)
            .Returns(Task.CompletedTask);

        _tokenService.Setup(x => x.GenerateAccessToken(user)).Returns("new-access-token");

        // Act
        var result = await _sut.RefreshAsync("old-refresh-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);

        Assert.NotNull(newToken);
        Assert.Equal(userId, newToken.UserId);
        Assert.Equal("new-hash", newToken.TokenHash);
        Assert.True(newToken.ExpiresAt > DateTime.UtcNow);

        _tokenService.Verify(x => x.TryRevokeRefreshTokenAsync(storedToken.Id), Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_TokenDoesNotExist_ReturnsNull()
    {
        // Arrange
        _tokenService.Setup(x => x.HashRefreshToken("invalid-token")).Returns("invalid-hash");

        _tokenService
            .Setup(x => x.GetRefreshTokenAsync("invalid-hash"))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _sut.RefreshAsync("invalid-token");

        // Assert
        Assert.Null(result);

        _userRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<UserRole?>()),
            Times.Never
        );

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<AuthResponseDto?>>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ReturnsNull()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RevokedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
        };

        _tokenService.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(token);

        // Act
        var result = await _sut.RefreshAsync("old-token");

        // Assert
        Assert.Null(result);

        _userRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<UserRole?>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };

        _tokenService.Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(token);

        // Act
        var result = await _sut.RefreshAsync("expired-token");

        // Assert
        Assert.Null(result);

        _userRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<UserRole?>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _userRepository.Setup(x => x.GetByIdAsync(token.UserId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.RefreshAsync("old-token");

        // Assert
        Assert.Null(result);

        _tokenService.Verify(x => x.TryRevokeRefreshTokenAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_InactiveUser_ReturnsNull()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _userRepository
            .Setup(x => x.GetByIdAsync(token.UserId))
            .ReturnsAsync(new User { Id = token.UserId, IsActive = false });

        // Act
        var result = await _sut.RefreshAsync("old-token");

        // Assert
        Assert.Null(result);

        _tokenService.Verify(x => x.TryRevokeRefreshTokenAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_RevocationFails_ReturnsNull()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _userRepository
            .Setup(x => x.GetByIdAsync(token.UserId))
            .ReturnsAsync(new User { Id = token.UserId, IsActive = true });

        _tokenService.Setup(x => x.TryRevokeRefreshTokenAsync(token.Id)).ReturnsAsync(false);

        // Act
        var result = await _sut.RefreshAsync("old-token");

        // Assert
        Assert.Null(result);

        _tokenService.Verify(x => x.CreateRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_TransactionFails_ThrowsException()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _userRepository
            .Setup(x => x.GetByIdAsync(token.UserId))
            .ReturnsAsync(new User { Id = token.UserId, IsActive = true });

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<AuthResponseDto?>>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.RefreshAsync("old-token"));
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _tokenService.Setup(x => x.TryRevokeRefreshTokenAsync(token.Id)).ReturnsAsync(true);

        // Act
        var result = await _sut.LogoutAsync("old-token");

        // Assert
        Assert.True(result);

        _tokenService.Verify(x => x.TryRevokeRefreshTokenAsync(token.Id), Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_TokenDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _tokenService.Setup(x => x.HashRefreshToken("old-token")).Returns("old-hash");

        _tokenService
            .Setup(x => x.GetRefreshTokenAsync("old-hash"))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _sut.LogoutAsync("old-token");

        // Assert
        Assert.False(result);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_AlreadyRevokedToken_ReturnsFalse()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        token.RevokedAt = DateTime.UtcNow;

        SetupRefreshTokenLookup(token);

        // Act
        var result = await _sut.LogoutAsync("old-token");

        // Assert
        Assert.False(result);

        _tokenService.Verify(x => x.TryRevokeRefreshTokenAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_RevocationFails_ReturnsFalse()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _tokenService.Setup(x => x.TryRevokeRefreshTokenAsync(token.Id)).ReturnsAsync(false);

        // Act
        var result = await _sut.LogoutAsync("old-token");

        // Assert
        Assert.False(result);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_SaveChangesFails_ThrowsException()
    {
        // Arrange
        var token = CreateValidRefreshToken();

        SetupRefreshTokenLookup(token);

        _tokenService.Setup(x => x.TryRevokeRefreshTokenAsync(token.Id)).ReturnsAsync(true);

        _unitOfWork.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.LogoutAsync("old-token"));
    }

    [Fact]
    public async Task LogoutAllAsync_UserExists_RevokesAllTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, TokenVersion = 0 };

        _userRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _sut.LogoutAllAsync(userId);
        // Assert
        Assert.Equal(1, user.TokenVersion);

        _tokenService.Verify(x => x.RevokeAllRefreshTokensAsync(userId), Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAllAsync_UserDoesNotExist_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        await _sut.LogoutAllAsync(userId);
        // Assert

        _tokenService.Verify(x => x.RevokeAllRefreshTokensAsync(It.IsAny<Guid>()), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LogoutAllAsync_TransactionFails_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.LogoutAllAsync(userId));
    }

    [Fact]
    public async Task LogoutAllAsync_SaveChangesFails_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, TokenVersion = 0 });

        _unitOfWork.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.LogoutAllAsync(userId));
    }

    private static RefreshToken CreateValidRefreshToken()
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "old-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(2),
        };
    }

    private void SetupRefreshTokenLookup(RefreshToken token)
    {
        _tokenService.Setup(x => x.HashRefreshToken("old-token")).Returns("old-hash");

        _tokenService.Setup(x => x.GetRefreshTokenAsync("old-hash")).ReturnsAsync(token);
    }
}
