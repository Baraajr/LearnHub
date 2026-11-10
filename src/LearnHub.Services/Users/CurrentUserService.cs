using System.Security.Claims;
using LearnHub.Data.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LearnHub.Services.Users;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string? UserId => accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
