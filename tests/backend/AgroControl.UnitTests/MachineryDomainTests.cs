using AgroControl.Domain.Modules.Machinery;

namespace AgroControl.UnitTests;

public sealed class MachineryDomainTests
{
    [Fact]
    public void Hour_meter_cannot_regress()
    {
        var machine = Machine.Create(Guid.NewGuid(), null, "Trator 1", "TR-001", MachineKind.Tractor, "Marca", "Modelo", 2025, 120m, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => machine.RecordHourMeter(119.9m, DateTime.UtcNow));
        Assert.Equal(120m, machine.CurrentHourMeter);
    }

    [Fact]
    public void Maintenance_total_cost_is_sum_of_components()
    {
        var maintenance = MaintenanceRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), MaintenanceKind.Preventive, "Revisão 500h",
            new DateOnly(2026, 9, 7), 500m, 400m, 250m, 50m,
            new DateOnly(2027, 1, 7), 750m, null, DateTime.UtcNow);

        Assert.Equal(700m, maintenance.TotalCost);
    }

    [Fact]
    public void Fueling_rejects_non_positive_quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fueling.Create(
            Guid.NewGuid(), Guid.NewGuid(), 0m, 100m, 10m, DateTime.UtcNow, null, DateTime.UtcNow));
    }
}
