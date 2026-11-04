using LearnHub.Data.Enums;

namespace LearnHub.Services.Instructors.DTOs;

public class InstructorResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public InstructorVerificationStatus VerificationStatus { get; set; }

    public Guid? ReviewedBy { get; set; }

    public string? ReviewerName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
}
