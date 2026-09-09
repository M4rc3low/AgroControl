using AgroControl.Application.Sync;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class OfflineSyncTransaction(AgroControlDbContext dbContext) : IOfflineSyncTransaction
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (dbContext.Database.CurrentTransaction is not null)
            return await action(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
