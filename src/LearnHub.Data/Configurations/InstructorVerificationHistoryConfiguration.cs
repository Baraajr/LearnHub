using LearnHub.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnHub.Data.Configurations;

public class InstructorVerificationHistoryConfiguration
    : IEntityTypeConfiguration<InstructorVerificationHistory>
{
    public void Configure(EntityTypeBuilder<InstructorVerificationHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder
            .Property(h => h.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(h => h.Reason).HasMaxLength(500).IsRequired(false);

        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasIndex(h => h.InstructorId);

        builder.HasIndex(h => h.AdminId);

        builder
            .HasOne(h => h.Instructor)
            .WithMany(ip => ip.VerificationHistory)
            .HasForeignKey(h => h.InstructorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(h => h.Admin)
            .WithMany()
            .HasForeignKey(h => h.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
