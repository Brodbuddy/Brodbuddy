namespace Application.Interfaces.Data;

public interface ITransactionManager
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation);
}