using System.ComponentModel.DataAnnotations;
using LearnHub.Data.Enums;

namespace LearnHub.Services.Auth.DTOs;

public class RegisterDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(UserRole))]
    [DeniedValues(
        UserRole.ADMIN,
        UserRole.SUPERADMIN,
        ErrorMessage = "The selected role is not allowed."
    )]
    public UserRole Role { get; set; }
}
