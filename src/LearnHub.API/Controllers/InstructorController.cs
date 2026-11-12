using System.Security.Claims;
using LearnHub.Data.Enums;
using LearnHub.Services.Instructors;
using LearnHub.Services.Instructors.DTOs;
using LearnHub.Services.Students.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.API.Controllers;

[ApiController]
[Route("api/instructors")]
public class InstructorsController(IInstructorsService instructorsService) : ControllerBase
{
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Invalid or missing user id claim.");
    }

    private const string AdminRoles = nameof(UserRole.SUPERADMIN) + "," + nameof(UserRole.ADMIN);

    [HttpGet]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetAll() => Ok(await instructorsService.GetAllAsync());

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetById(Guid id) =>
        Ok(await instructorsService.GetInstructorByIdAsync(id));

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> UpdateRole(Guid id, ChangeRoleDto dto)
    {
        await instructorsService.UpdateRoleAsync(GetCurrentUserId(), id, dto.Role);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInstructorStatusDto dto)
    {
        await instructorsService.UpdateStatusAsync(GetCurrentUserId(), id, dto);
        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetHistory(Guid id) =>
        Ok(await instructorsService.GetVerificationHistoryAsync(id));

    [Authorize(Roles = AdminRoles)]
    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var student = await instructorsService.SetActiveAsync(id, true);
        return Ok(student);
    }

    [Authorize(Roles = AdminRoles)]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var student = await instructorsService.SetActiveAsync(id, false);
        return Ok(student);
    }
}
