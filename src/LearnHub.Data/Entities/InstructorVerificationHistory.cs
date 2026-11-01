using LearnHub.Data.Enums;

namespace LearnHub.Data.Entities;

public class InstructorVerificationHistory
{
    public Guid Id { get; set; }

    public Guid InstructorId { get; set; }

    public Guid AdminId { get; set; }

    public InstructorVerificationStatus? PreviousStatus { get; set; }

    public InstructorVerificationStatus NewStatus { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public InstructorProfile Instructor { get; set; } = null!;

    public User Admin { get; set; } = null!;
}
