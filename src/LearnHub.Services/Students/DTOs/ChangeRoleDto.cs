using System.ComponentModel.DataAnnotations;
using LearnHub.Data.Enums;

namespace LearnHub.Services.Students.DTOs;

public class ChangeRoleDto
{
    [Required]
    [EnumDataType(typeof(UserRole))]
    [DeniedValues(UserRole.ADMIN, UserRole.SUPERADMIN)]
    public UserRole Role { get; set; }
}
