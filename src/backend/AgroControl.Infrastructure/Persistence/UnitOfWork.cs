using AgroControl.Application.Common;

namespace AgroControl.Infrastructure.Persistence;

public sealed class UnitOfWork(AgroControlDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
