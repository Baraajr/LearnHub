using System.Text;
using LearnHub.Repositories;
using LearnHub.Services.Admins;
using LearnHub.Services.Admins.Interfaces;
using LearnHub.Services.Auth;
using LearnHub.Services.Auth.Interfaces;
using LearnHub.Services.Instructors;
using LearnHub.Services.Instructors.Interfaces;
using LearnHub.Services.Options;
using LearnHub.Services.Students;
using LearnHub.Services.Students.Interfaces;
using LearnHub.Services.Users;
using LearnHub.Services.Users.Interfaces;
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

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IInstructorsService, InstructorsService>();
        services.AddScoped<IStudentsService, StudentsService>();

        return services;
    }
}
