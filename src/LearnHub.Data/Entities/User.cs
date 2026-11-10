using System.Text.Json.Serialization;
using LearnHub.Data.Enums;
using LearnHub.Data.Interfaces;

namespace LearnHub.Data.Entities;

public class User : ITrackable
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public int TokenVersion { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public StudentProfile? StudentProfile { get; set; }

    public InstructorProfile? InstructorProfile { get; set; }

    [JsonIgnore]
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
