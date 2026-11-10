using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.UserName).HasMaxLength(256).IsRequired();

        builder.Property(u => u.NormalizedUserName).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();

        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();

        builder.HasIndex(u => u.NormalizedEmail).IsUnique();

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(u => u.IsActive).HasDefaultValue(true).IsRequired();

        builder.Property(u => u.TokenVersion).HasDefaultValue(0).IsRequired();

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired(false);

        var validRoles = string.Join(", ", Enum.GetNames<UserRole>().Select(role => $"'{role}'"));

        builder.ToTable(t => t.HasCheckConstraint("CK_Users_Role", $"[Role] IN ({validRoles})"));
    }
}
