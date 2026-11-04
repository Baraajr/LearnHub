using LearnHub.Data.Context;
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
        var connectionString = configuration["DB_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DB_CONNECTION_STRING is not configured.");

        services.AddDbContext<LearnHubDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
        );

        return services;
    }
}
