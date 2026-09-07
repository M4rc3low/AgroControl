using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;

namespace AgroControl.Application.Subscriptions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationModuleEntitlement?> GetOverrideAsync(Guid organizationId, ModuleKey module, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationModuleEntitlement>> GetOverridesAsync(Guid organizationId, CancellationToken cancellationToken = default);
    void Add(Subscription subscription);
}
