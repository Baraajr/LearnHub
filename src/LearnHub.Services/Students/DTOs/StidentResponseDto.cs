namespace LearnHub.Services.Students.DTOs;

public class StudentResponseDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Student profile audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // User account audit
    public DateTime AccountCreatedAt { get; set; }
    public string? AccountCreatedBy { get; set; }
    public DateTime? AccountUpdatedAt { get; set; }
    public string? AccountUpdatedBy { get; set; }
}
