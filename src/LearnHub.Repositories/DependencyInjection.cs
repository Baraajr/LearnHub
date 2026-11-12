using LearnHub.Data;
using LearnHub.Repositories.InstructorProfiles;
using LearnHub.Repositories.InstructorVerificationHistories;
using LearnHub.Repositories.RefreshTokens;
using LearnHub.Repositories.StudentProfiles;
using LearnHub.Repositories.UnitOfWorkRepo;
using LearnHub.Repositories.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddData(configuration);

        services.Scan(scan =>
            scan.FromAssemblyOf<IUserRepository>()
                .AddClasses(c => c.AssignableTo<IScopedRepository>())
                .As(t => t.GetInterfaces().Where(i => i != typeof(IScopedRepository)))
                .WithScopedLifetime()
        );

        return services;
    }
}
