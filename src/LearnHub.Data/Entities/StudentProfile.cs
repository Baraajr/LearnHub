using LearnHub.Data.Interfaces;

namespace LearnHub.Data.Entities;

public class StudentProfile : ITrackable
{
    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
