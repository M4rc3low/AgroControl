using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;

namespace AgroControl.Application.Subscriptions;

public sealed class PlanEntitlementCatalog
{
    private static readonly HashSet<ModuleKey> BasicModules =
    [
        ModuleKey.Identity,
        ModuleKey.Organizations,
        ModuleKey.Farms,
        ModuleKey.Fields,
        ModuleKey.Crops,
        ModuleKey.Seasons,
        ModuleKey.Inventory,
        ModuleKey.Finance
    ];

    private static readonly HashSet<ModuleKey> ProModules =
    [
        .. BasicModules,
        ModuleKey.Machinery,
        ModuleKey.Market,
        ModuleKey.PrecisionAgriculture,
        ModuleKey.Irrigation,
        ModuleKey.Sustainability
    ];

    private static readonly HashSet<ModuleKey> IntelligenceModules =
    [
        .. ProModules,
        ModuleKey.Intelligence,
        ModuleKey.Telemetry
    ];

    public bool Includes(PlanCode plan, ModuleKey module) => plan switch
    {
        PlanCode.Basic => BasicModules.Contains(module),
        PlanCode.Pro => ProModules.Contains(module),
        PlanCode.Intelligence => IntelligenceModules.Contains(module),
        PlanCode.Enterprise => true,
        _ => false
    };
}
