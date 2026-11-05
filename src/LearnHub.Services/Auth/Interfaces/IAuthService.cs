using LearnHub.Services.Auth.DTOs;

namespace LearnHub.Services.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto request);
    Task<AuthResponseDto> LoginAsync(LoginDto request);
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task LogoutAllAsync(Guid userId);
}
