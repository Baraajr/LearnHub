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
        services
            .AddOptions<DatabaseOptions>()
            .Configure(options =>
            {
                options.Host = configuration["DB_HOST"] ?? "";
                options.Port = int.TryParse(configuration["DB_PORT"], out var port) ? port : 1433;
                options.Name = configuration["DB_NAME"] ?? "";
                options.Username = configuration["DB_USERNAME"] ?? "";
                options.Password = configuration["DB_PASSWORD"] ?? "";
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "DB_HOST is required.")
            .Validate(
                options => options.Port > 0 && options.Port <= 65535,
                "DB_PORT must be a valid port."
            )
            .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "DB_NAME is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Username),
                "DB_USERNAME is required."
            )
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                "DB_PASSWORD is required."
            )
            .ValidateOnStart();

        services.AddDbContext<LearnHubDbContext>(
            (provider, options) =>
            {
                var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                var connectionString =
                    $"Server={database.Host},{database.Port};"
                    + $"Database={database.Name};"
                    + $"User Id={database.Username};"
                    + $"Password={database.Password};"
                    + "TrustServerCertificate=True;";

                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            }
        );

        return services;
    }
}
