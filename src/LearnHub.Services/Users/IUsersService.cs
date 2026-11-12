using LearnHub.Data.Entities;
using LearnHub.Data.Enums;

namespace LearnHub.Services.Users;

public interface IUsersService
{
    Task<List<User>> GetAllAsync(UserRole? role = null);

    Task<User> GetUserByIdAsync(Guid userId, UserRole? role = null);

    Task<User> SetActiveStatusAsync(Guid userId, bool isActive);

    Task UpdateRoleAsync(Guid adminId, Guid userId, UserRole newRole);
    Task<bool> ExistsAsync(Guid userId, UserRole? role = null);
}
