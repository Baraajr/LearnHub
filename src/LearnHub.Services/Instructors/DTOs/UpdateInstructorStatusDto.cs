using System.ComponentModel.DataAnnotations;
using LearnHub.Data.Enums;

namespace LearnHub.Services.Instructors.DTOs;

public class UpdateInstructorStatusDto : IValidatableObject
{
    [Required]
    public InstructorVerificationStatus? Status { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
            yield return new ValidationResult("Invalid verification status.", [nameof(Status)]);

        if (
            Status == InstructorVerificationStatus.REJECTED
            && string.IsNullOrWhiteSpace(RejectionReason)
        )
            yield return new ValidationResult(
                "Rejection reason is required.",
                [nameof(RejectionReason)]
            );
    }
}
