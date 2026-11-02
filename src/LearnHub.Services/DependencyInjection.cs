using System.Text;
using LearnHub.Repositories;
using LearnHub.Services.Auth;
using LearnHub.Services.Auth.Interfaces;
using LearnHub.Services.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddRepositories(configuration);

        // JwtOptions
        services
            .AddOptions<JwtOptions>()
            .Configure(options =>
            {
                options.Issuer = configuration["JWT_ISSUER"] ?? "";
                options.Audience = configuration["JWT_AUDIENCE"] ?? "";
                options.Secret = configuration["JWT_SECRET"] ?? "";

                options.AccessTokenLifetimeMinutes = int.TryParse(
                    configuration["JWT_ACCESS_TOKEN_EXPIRATION_MINUTES"],
                    out var accessMinutes
                )
                    ? accessMinutes
                    : 15;

                options.RefreshTokenLifetimeDays = int.TryParse(
                    configuration["JWT_REFRESH_TOKEN_EXPIRATION_DAYS"],
                    out var refreshDays
                )
                    ? refreshDays
                    : 7;
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "JWT Issuer is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "JWT Audience is required")
            .Validate(
                o =>
                    !string.IsNullOrWhiteSpace(o.Secret)
                    && Encoding.UTF8.GetByteCount(o.Secret) >= 32,
                "JWT Secret must be at least 32 bytes"
            )
            .Validate(
                o => o.AccessTokenLifetimeMinutes > 0,
                "Access token expiration must be greater than zero."
            )
            .Validate(
                o => o.RefreshTokenLifetimeDays > 0,
                "Refresh tokens expiration must be greater than zero."
            )
            .ValidateOnStart();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
