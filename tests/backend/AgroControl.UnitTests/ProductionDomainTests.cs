using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.UnitTests;

public sealed class ProductionDomainTests
{
    [Fact]
    public void Farm_requires_positive_area()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Farm.Create(Guid.NewGuid(), "Fazenda Teste", 0m, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Field_can_be_soft_deactivated()
    {
        var field = Field.Create(Guid.NewGuid(), Guid.NewGuid(), "Talhão Norte", 42.5m, DateTime.UtcNow);
        field.Deactivate(DateTime.UtcNow.AddMinutes(1));
        Assert.False(field.IsActive);
    }

    [Fact]
    public void Season_rejects_end_date_before_start_date()
    {
        Assert.Throws<ArgumentException>(() =>
            Season.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Safra 2026/27",
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 9, 30),
                60m,
                DateTime.UtcNow));
    }
}
