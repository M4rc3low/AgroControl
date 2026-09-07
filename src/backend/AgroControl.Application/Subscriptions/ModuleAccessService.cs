using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;

namespace AgroControl.Application.Subscriptions;

public sealed record ModuleAccessSnapshot(
    string? Plan,
    IReadOnlyDictionary<ModuleKey, bool> Modules);

public sealed class ModuleAccessService
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly PlanEntitlementCatalog _catalog;

    public ModuleAccessService(ISubscriptionRepository subscriptions, PlanEntitlementCatalog catalog)
    {
        _subscriptions = subscriptions;
        _catalog = catalog;
    }

    public async Task<bool> HasAccessAsync(
        Guid organizationId,
        ModuleKey module,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByOrganizationAsync(organizationId, cancellationToken);
        if (subscription is null || !subscription.IsActive(DateTime.UtcNow))
            return false;

        var entitlementOverride = await _subscriptions.GetOverrideAsync(organizationId, module, cancellationToken);
        return entitlementOverride?.IsEnabled ?? _catalog.Includes(subscription.Plan, module);
    }

    public async Task<ModuleAccessSnapshot> GetSnapshotAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByOrganizationAsync(organizationId, cancellationToken);
        var overrides = await _subscriptions.GetOverridesAsync(organizationId, cancellationToken);
        var overrideMap = overrides.ToDictionary(item => item.Module, item => item.IsEnabled);
        var active = subscription is not null && subscription.IsActive(DateTime.UtcNow);

        var modules = Enum.GetValues<ModuleKey>().ToDictionary(
            module => module,
            module => active && (overrideMap.TryGetValue(module, out var enabled)
                ? enabled
                : _catalog.Includes(subscription!.Plan, module)));

        return new ModuleAccessSnapshot(subscription?.Plan.ToString(), modules);
    }
}
