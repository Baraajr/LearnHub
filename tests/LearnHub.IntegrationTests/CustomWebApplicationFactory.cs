using LearnHub.Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DB_HOST"] = "localhost",
                        ["DB_PORT"] = "1433",
                        ["DB_NAME"] = "LearnHub_Test",
                        ["DB_USERNAME"] = "learnhub_test_user",
                        ["DB_PASSWORD"] = "StrongTestPassword123!",
                        ["JWT_ISSUER"] = "LearnHub.Tests",
                        ["JWT_AUDIENCE"] = "LearnHub.Tests",
                        ["JWT_SECRET"] = "LearnHub_Test_Secret_Key_Must_Be_At_Least_32_Bytes",
                        ["JWT_ACCESS_TOKEN_EXPIRATION_MINUTES"] = "15",
                        ["JWT_REFRESH_TOKEN_EXPIRATION_DAYS"] = "7",
                    }
                );
            }
        );

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<LearnHubDbContext>();

            db.Database.Migrate();
        });
    }
}
