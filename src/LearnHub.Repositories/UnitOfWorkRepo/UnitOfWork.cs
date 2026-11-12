using LearnHub.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Repositories.UnitOfWorkRepo;

public class UnitOfWork(LearnHubDbContext context) : IUnitOfWork, IScopedRepository
{
    private readonly LearnHubDbContext _context = context;

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var result = await operation();

            await transaction.CommitAsync();

            return result;
        });
    }
}
