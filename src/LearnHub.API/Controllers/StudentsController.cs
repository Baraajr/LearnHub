using LearnHub.Data.Entities;
using LearnHub.Data.Enums;
using LearnHub.Services.Students;
using LearnHub.Services.Students.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.API.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(IStudentsService studentsService) : ControllerBase
{
    private const string AdminRoles = nameof(UserRole.SUPERADMIN) + "," + nameof(UserRole.ADMIN);

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await studentsService.GetAllAsync());

    [Authorize(Roles = AdminRoles)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) =>
        Ok(await studentsService.GetStudentByIdAsync(id));

    [Authorize(Roles = AdminRoles)]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, ChangeRoleDto dto)
    {
        if (HttpContext.Items["User"] is not User user)
            return Unauthorized();

        return Ok(await studentsService.UpdateRoleAsync(user.Id, id, dto));
    }

    [Authorize(Roles = AdminRoles)]
    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var student = await studentsService.SetActiveStatus(id, true);
        return Ok(student);
    }

    [Authorize(Roles = AdminRoles)]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var student = await studentsService.SetActiveStatus(id, false);
        return Ok(student);
    }
}
