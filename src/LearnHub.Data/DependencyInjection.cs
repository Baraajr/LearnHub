using LearnHub.Data.Context;
using LearnHub.Data.Interceptors;
using LearnHub.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LearnHub.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            "workstation id=learnhub.mssql.somee.com;packet size=4096;user id=baraajr1_SQLLogin_1;pwd=test1234;data source=learnhub.mssql.somee.com;persist security info=False;initial catalog=learnhub;TrustServerCertificate=True";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DB_CONNECTION_STRING is not configured.");

        services.AddDbContext<LearnHubDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
        );
        services.AddHttpContextAccessor();

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
