using System.ComponentModel.DataAnnotations;
using LearnHub.Data.Enums;

namespace LearnHub.Services.Students.DTOs;

public class ChangeRoleDto
{
    [Required]
    [EnumDataType(typeof(UserRole))]
    [DeniedValues(
        UserRole.ADMIN,
        UserRole.SUPERADMIN,
        ErrorMessage = "The selected role is not allowed."
    )]
    public UserRole Role { get; set; }
}
