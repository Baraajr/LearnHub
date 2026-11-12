using LearnHub.Data.Entities;

namespace LearnHub.Repositories.Users;

public interface IUserRepository
{
    IQueryable<User> GetAllAsync();

    Task<User?> GetByIdAsync(Guid id);

    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
