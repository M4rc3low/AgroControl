using AgroControl.Domain.Platform;

namespace AgroControl.Domain.Modules.Subscriptions;

public sealed class OrganizationModuleEntitlement
{
    private OrganizationModuleEntitlement() { }

    private OrganizationModuleEntitlement(
        Guid organizationId,
        ModuleKey module,
        bool isEnabled,
        DateTime updatedAtUtc)
    {
        OrganizationId = organizationId;
        Module = module;
        IsEnabled = isEnabled;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid OrganizationId { get; private set; }
    public ModuleKey Module { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static OrganizationModuleEntitlement Create(
        Guid organizationId,
        ModuleKey module,
        bool isEnabled,
        DateTime updatedAtUtc) =>
        new(organizationId, module, isEnabled, updatedAtUtc);
}
