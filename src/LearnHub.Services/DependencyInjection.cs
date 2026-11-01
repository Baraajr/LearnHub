using System.Text;
using LearnHub.Repositories;
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
                options.ExpirationSeconds = int.TryParse(
                    configuration["JWT_EXPIRATION_SECONDS"],
                    out var expirationSeconds
                )
                    ? expirationSeconds
                    : 87400.00f;
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "JWT Issuer is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "JWT Audience is required")
            .Validate(
                o =>
                    !string.IsNullOrWhiteSpace(o.Secret)
                    && Encoding.UTF8.GetByteCount(o.Secret) >= 32,
                "JWT Secret must be at least 32 bytes"
            )
            .Validate(o => o.ExpirationSeconds > 0, "JWT expiration must be greater than zero.")
            .ValidateOnStart();

        // Register services here.

        return services;
    }
}
