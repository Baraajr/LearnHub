using System.Text;
using LearnHub.Data.Context;
using LearnHub.Data.Entities;
using LearnHub.Repositories;
using LearnHub.Services.Auth;
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
        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<LearnHubDbContext>();
        // JwtOptions
        // JWT Options
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
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
                "Refresh token expiration must be greater than zero."
            )
            .ValidateOnStart();

        services.Scan(scan =>
            scan.FromAssemblyOf<IAuthService>()
                .AddClasses(c => c.AssignableTo<IScopedService>())
                .As(t => t.GetInterfaces().Where(i => i != typeof(IScopedService)))
                .WithScopedLifetime()
        );

        return services;
    }
}
