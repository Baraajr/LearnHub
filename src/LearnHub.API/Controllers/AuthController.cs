using System.Security.Claims;
using LearnHub.Data.Entities;
using LearnHub.Services.Auth;
using LearnHub.Services.Auth.DTOs;
using LearnHub.Services.Exceptions;
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
        if (User.Identity?.IsAuthenticated == true)
            throw new ForbiddenException("You are already logged in.");
        var result = await _authService.RegisterAsync(request);

        if (result.AccessToken is not null && result.RefreshToken is not null)
        {
            SetAccessCookie(result.AccessToken);
            SetRefreshCookie(result.RefreshToken);
        }

        return Ok(
            new
            {
                message = result.AccessToken is null
                    ? "Registration successful. Your instructor account is awaiting verification."
                    : "Registered successfully.",
            }
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var result = await _authService.LoginAsync(request);

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
            DeleteRefreshCookie();
            DeleteAccessCookie();
            throw new UnauthorizedException("Refresh token is missing.");
        }

        var result = await _authService.RefreshAsync(refreshToken);

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
            throw new UnauthorizedException("Invalid user identity.");

        await _authService.LogoutAllAsync(userId);

        DeleteAccessCookie();
        DeleteRefreshCookie();

        return Ok(new { message = "Logged out from all devices." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var user =
            HttpContext.Items["User"] as User
            ?? throw new UnauthorizedException("Please login first");
        return Ok(MapToDto(user));
    }

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
            UserName = user.UserName ?? throw new InvalidOperationException("UserName is missing."),
            Email = user.Email ?? throw new InvalidOperationException("Email is missing."),
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            UpdatedAt = user.UpdatedAt,
            UpdatedBy = user.UpdatedBy,
        };
    }
}
