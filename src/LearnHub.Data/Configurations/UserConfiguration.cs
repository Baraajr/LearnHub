using LearnHub.Data.Entities;
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
    }
}
