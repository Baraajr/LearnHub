using LearnHub.Data.Context;
using LearnHub.Repositories.Interfaces;

namespace LearnHub.Repositories.Implementations;

public class UnitOfWork(LearnHubDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync()
    {
        return context.SaveChangesAsync();
    }
}
