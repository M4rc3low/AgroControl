using AgroControl.Application.Subscriptions;
using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class SubscriptionRepository(AgroControlDbContext dbContext) : ISubscriptionRepository
{
    public Task<Subscription?> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Subscriptions.SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);

    public Task<OrganizationModuleEntitlement?> GetOverrideAsync(
        Guid organizationId,
        ModuleKey module,
        CancellationToken cancellationToken = default) =>
        dbContext.OrganizationModuleEntitlements.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Module == module,
            cancellationToken);

    public async Task<IReadOnlyList<OrganizationModuleEntitlement>> GetOverridesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.OrganizationModuleEntitlements
            .Where(item => item.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public void Add(Subscription subscription) => dbContext.Subscriptions.Add(subscription);
}
