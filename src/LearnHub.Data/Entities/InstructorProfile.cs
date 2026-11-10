using LearnHub.Data.Enums;
using LearnHub.Data.Interfaces;

namespace LearnHub.Data.Entities;

public class InstructorProfile : ITrackable
{
    public Guid UserId { get; set; }

    public InstructorVerificationStatus VerificationStatus { get; set; } =
        InstructorVerificationStatus.PENDING;
    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public User User { get; set; } = null!;

    public User? Reviewer { get; set; }

    public ICollection<InstructorVerificationHistory> VerificationHistory { get; set; } =
        new List<InstructorVerificationHistory>();
}
