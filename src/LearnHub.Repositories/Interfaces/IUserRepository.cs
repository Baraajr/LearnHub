using LearnHub.Data.Entities;
using LearnHub.Data.Enums;

namespace LearnHub.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);

    IQueryable<User> GetAll(UserRole? role = null);

    Task<int> CountAsync(UserRole? role = null);

    Task AddAsync(User user);
    void Update(User user);
}
