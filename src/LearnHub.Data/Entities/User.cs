using System.Text.Json.Serialization;
using LearnHub.Data.Enums;
using LearnHub.Data.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LearnHub.Data.Entities;

public class User : IdentityUser<Guid>, ITrackable
{
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
