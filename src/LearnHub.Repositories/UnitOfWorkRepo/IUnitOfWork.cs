namespace LearnHub.Repositories.UnitOfWorkRepo;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
