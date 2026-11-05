using LearnHub.Data.Entities;
using LearnHub.Data.Enums;

namespace LearnHub.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllSync(UserRole? role = null);
    Task<User?> GetByIdAsync(Guid id, UserRole? role = null);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email, Guid? exceptUserId = null);

    Task<int> CountActiveAsync(UserRole? role = null);

    Task AddAsync(User user);
    void Update(User user);
    Task<bool> ExistsAsync(Guid userId, UserRole? role = null);
}
