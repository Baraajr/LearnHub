using LearnHub.Data.Enums;

namespace LearnHub.Services.Instructors.DTOs;

public class InstructorVerificationHistoryDto
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public InstructorVerificationStatus? PreviousStatus { get; set; }
    public InstructorVerificationStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
