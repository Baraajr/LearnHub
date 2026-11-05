using LearnHub.Data.Enums;
using LearnHub.Services.Admins.DTOs;
using LearnHub.Services.Admins.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.API.Controllers;

[ApiController]
[Route("api/admins")]
[Authorize(Roles = nameof(UserRole.SUPERADMIN))]
public class AdminsController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var admins = await adminService.GetAllAsync();
        return Ok(admins);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var admin = await adminService.GetByIdAsync(id);
        return Ok(admin);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAdminDto dto)
    {
        var admin = await adminService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = admin.Id }, admin);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await adminService.ActivateAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await adminService.DeleteAsync(id);
        return NoContent();
    }
}
