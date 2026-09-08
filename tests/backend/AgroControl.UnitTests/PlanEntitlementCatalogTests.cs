using AgroControl.Application.Subscriptions;
using AgroControl.Domain.Modules.Subscriptions;
using AgroControl.Domain.Platform;

namespace AgroControl.UnitTests;

public sealed class PlanEntitlementCatalogTests
{
    private readonly PlanEntitlementCatalog _catalog = new();

    [Fact]
    public void Basic_includes_core_management_modules()
    {
        Assert.True(_catalog.Includes(PlanCode.Basic, ModuleKey.Farms));
        Assert.True(_catalog.Includes(PlanCode.Basic, ModuleKey.Finance));
        Assert.False(_catalog.Includes(PlanCode.Basic, ModuleKey.Intelligence));
        Assert.False(_catalog.Includes(PlanCode.Basic, ModuleKey.Commercial));
        Assert.False(_catalog.Includes(PlanCode.Basic, ModuleKey.Export));
    }

    [Fact]
    public void Pro_includes_commercial_module()
    {
        Assert.True(_catalog.Includes(PlanCode.Pro, ModuleKey.Commercial));
    }

    [Fact]
    public void Enterprise_includes_every_module()
    {
        foreach (var module in Enum.GetValues<ModuleKey>())
            Assert.True(_catalog.Includes(PlanCode.Enterprise, module));
    }
}
