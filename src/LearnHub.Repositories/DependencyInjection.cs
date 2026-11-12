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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInstructorProfileRepository, InstructorProfileRepository>();
        services.AddScoped<
            IInstructorVerificationHistoryRepository,
            InstructorVerificationHistoryRepository
        >();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();

        return services;
    }
}
