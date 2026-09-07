using AgroControl.Domain.Modules.Machinery;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class MachineryPersistenceTests
{
    [Fact]
    public async Task Machinery_repository_isolates_tenants_and_calculates_operational_costs()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Machinery A", $"machinery-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Machinery B", $"machinery-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(organizationA, organizationB);

        var machineA = Machine.Create(organizationA.Id, null, "Trator A", "TR-A", MachineKind.Tractor, "Marca", "A1", 2025, 100m, now);
        var machineB = Machine.Create(organizationB.Id, null, "Trator B", "TR-B", MachineKind.Tractor, "Marca", "B1", 2025, 10m, now);
        dbContext.Machines.AddRange(machineA, machineB);
        dbContext.HourMeterReadings.AddRange(
            HourMeterReading.Create(organizationA.Id, machineA.Id, 100m, now.AddDays(-10), null, now),
            HourMeterReading.Create(organizationA.Id, machineA.Id, 120m, now, null, now));
        dbContext.Fuelings.Add(Fueling.Create(organizationA.Id, machineA.Id, 50m, 300m, 120m, now, null, now));
        dbContext.MaintenanceRecords.Add(MaintenanceRecord.Create(
            organizationA.Id, machineA.Id, MaintenanceKind.Preventive, "Revisão", DateOnly.FromDateTime(now), 120m,
            100m, 40m, 10m, null, 150m, null, now));
        await dbContext.SaveChangesAsync();

        var repository = new MachineryRepository(dbContext);
        var (itemsA, totalA) = await repository.ListMachinesAsync(organizationA.Id, 0, 20, null, null, null);
        var (itemsB, totalB) = await repository.ListMachinesAsync(organizationB.Id, 0, 20, null, null, null);
        var totals = await repository.GetCostTotalsAsync(organizationA.Id, machineA.Id);

        Assert.Single(itemsA);
        Assert.Single(itemsB);
        Assert.Equal(1, totalA);
        Assert.Equal(1, totalB);
        Assert.Equal(machineA.Id, itemsA[0].Id);
        Assert.Equal(50m, totals.FuelLiters);
        Assert.Equal(300m, totals.FuelCost);
        Assert.Equal(150m, totals.MaintenanceCost);
        Assert.Equal(100m, totals.MinimumTrackedHour);
        Assert.Equal(120m, totals.MaximumTrackedHour);
    }
}
