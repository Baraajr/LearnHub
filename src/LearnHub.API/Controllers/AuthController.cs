using System.Security.Claims;
using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Services.Auth.DTOs;
using LearnHub.Services.Auth.Interfaces;
using LearnHub.Services.Options; // Ensure this points to your options namespace
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LearnHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IWebHostEnvironment environment,
    IOptions<JwtOptions> jwtOptions
) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        var result = await _authService.RegisterAsync(request);

        if (result is null)
            return Conflict(new { message = "Email already exists." });

        SetAccessCookie(result.AccessToken);
        SetRefreshCookie(result.RefreshToken);

        return Ok(new { message = "Registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var result = await _authService.LoginAsync(request);

        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });

        SetAccessCookie(result.AccessToken);
        SetRefreshCookie(result.RefreshToken);

        return Ok(new { message = "Logged in successfully." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (
            !Request.Cookies.TryGetValue("refreshToken", out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken)
        )
        {
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        var result = await _authService.RefreshAsync(refreshToken);

        if (result is null)
        {
            DeleteRefreshCookie();
            DeleteAccessCookie();
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        SetAccessCookie(result.AccessToken);
        SetRefreshCookie(result.RefreshToken);

        return Ok(new { message = "Token refreshed successfully." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (
            Request.Cookies.TryGetValue("refreshToken", out var refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken)
        )
        {
            await _authService.LogoutAsync(refreshToken);
        }

        DeleteRefreshCookie();
        DeleteAccessCookie();

        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _authService.LogoutAllAsync(userId);

        DeleteAccessCookie();
        DeleteRefreshCookie();

        return Ok(new { message = "Logged out from all devices." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var user = HttpContext.Items["User"] as User;

        if (user is null)
            return Unauthorized();

        return Ok(MapToDto(user));
    }

    // --- DRY COOKIE HELPERS ---

    private CookieOptions CreateCookieOptions(string path, DateTimeOffset? expires = null)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = path,
            Expires = expires,
        };
    }

    private void SetAccessCookie(string token)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        Response.Cookies.Append("access_token", token, CreateCookieOptions("/", expires));
    }

    private void SetRefreshCookie(string token)
    {
        var expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays);
        Response.Cookies.Append("refreshToken", token, CreateCookieOptions("/api/auth", expires));
    }

    private void DeleteAccessCookie()
    {
        Response.Cookies.Delete("access_token", CreateCookieOptions("/"));
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete("refreshToken", CreateCookieOptions("/api/auth"));
    }

    private static UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
        };
    }
}
