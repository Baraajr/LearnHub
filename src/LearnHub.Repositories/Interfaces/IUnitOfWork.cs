namespace LearnHub.Repositories.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
