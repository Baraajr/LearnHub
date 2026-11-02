using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(u => u.IsActive).HasDefaultValue(true).IsRequired();

        builder.Property(u => u.TokenVersion).HasDefaultValue(0).IsRequired();

        builder.Property(u => u.CreatedAt).IsRequired();

        builder.Property(u => u.UpdatedAt).IsRequired(false);

        // database let undefined roles through
        // so we make db validation level
        // we also validated in service
        var validRoles = string.Join(", ", Enum.GetNames<UserRole>().Select(role => $"'{role}'"));

        builder.ToTable(t => t.HasCheckConstraint("CK_Users_Role", $"[Role] IN ({validRoles})"));
    }
}
