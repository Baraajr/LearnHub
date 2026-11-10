using LearnHub.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Data.Context;

public class LearnHubDbContext : IdentityUserContext<User, Guid>
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();

    public DbSet<InstructorVerificationHistory> InstructorVerificationHistories =>
        Set<InstructorVerificationHistory>();

    public LearnHubDbContext(DbContextOptions<LearnHubDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearnHubDbContext).Assembly);
    }
}
