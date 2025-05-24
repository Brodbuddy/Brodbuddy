using Application.Interfaces.Data;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class EfTransactionManager : ITransactionManager
{
    private readonly PgDbContext _dbContext;

    public EfTransactionManager(PgDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction != null) return await operation();
        
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex.GetBaseException();
            }
        });
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction != null) await operation();
        
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