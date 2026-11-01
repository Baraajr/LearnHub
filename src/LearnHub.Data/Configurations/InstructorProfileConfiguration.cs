using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

public class InstructorProfileConfiguration : IEntityTypeConfiguration<InstructorProfile>
{
    public void Configure(EntityTypeBuilder<InstructorProfile> builder)
    {
        builder.HasKey(ip => ip.UserId);

        builder
            .Property(ip => ip.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(InstructorVerificationStatus.PENDING)
            .HasSentinel(InstructorVerificationStatus.PENDING)
            .IsRequired();

        builder.Property(ip => ip.RejectionReason).HasMaxLength(500).IsRequired(false);

        builder.Property(ip => ip.CreatedAt).IsRequired();

        builder.Property(ip => ip.UpdatedAt).IsRequired(false);

        builder.HasIndex(ip => ip.VerificationStatus);

        builder
            .HasOne(ip => ip.User)
            .WithOne(u => u.InstructorProfile)
            .HasForeignKey<InstructorProfile>(ip => ip.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ip => ip.Reviewer)
            .WithMany()
            .HasForeignKey(ip => ip.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
