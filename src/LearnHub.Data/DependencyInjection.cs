using LearnHub.Data.Context;
using LearnHub.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<LearnHubDbContext>(
            (sp, options) =>
                options
                    .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
                    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>())
        );

        return services;
    }
}
