using LearnHub.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.HasKey(sp => sp.UserId);

        builder.Property(sp => sp.CreatedAt).IsRequired();

        builder.Property(sp => sp.UpdatedAt).IsRequired(false);

        builder
            .HasOne(sp => sp.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
