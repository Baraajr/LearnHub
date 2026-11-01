namespace LearnHub.Data.Entities;

public class StudentProfile
{
    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
