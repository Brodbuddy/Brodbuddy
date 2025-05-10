using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class EfTransactionManager : ITransactionManager
{
    private readonly DbContext _dbContext;

    public EfTransactionManager(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}