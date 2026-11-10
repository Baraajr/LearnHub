using System.ComponentModel.DataAnnotations;

namespace LearnHub.Services.Admins.DTOs;

public class CreateAdminDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
