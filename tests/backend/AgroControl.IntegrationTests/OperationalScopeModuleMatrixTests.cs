using AgroControl.Application.Commercial;
using AgroControl.Application.Exporting;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Commercial;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Exporting;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Inventory;
using AgroControl.Domain.Modules.Irrigation;
using AgroControl.Domain.Modules.Machinery;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Domain.Modules.Sustainability;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class OperationalScopeModuleMatrixTests
{
    [Fact]
    public async Task Restricted_scope_blocks_farm_b_across_inventory_machinery_irrigation_sustainability_export_and_commercial()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Guid organizationId;
        Guid farmAId;
        Guid farmBId;
        Guid warehouseAId;
        Guid warehouseBId;
        Guid movementAId;
        Guid movementBId;
        Guid machineAId;
        Guid machineBId;
        Guid irrigationAId;
        Guid irrigationBId;
        Guid emissionAId;
        Guid emissionBId;
        Guid exportAId;
        Guid exportBId;
        Guid opportunityAId;
        Guid opportunityBId;

        await using (var seed = new AgroControlDbContext(options))
        {
            await seed.Database.EnsureDeletedAsync();
            await seed.Database.MigrateAsync();

            var now = DateTime.UtcNow;
            var organization = Organization.Create("Grupo Escopo Horizontal", $"scope-modules-{Guid.NewGuid():N}", now);
            var farmA = Farm.Create(organization.Id, "Fazenda A", 220m, "Campinas", "SP", now, null, "BR", "SP", null, null, -22.90m, -47.06m, "America/Sao_Paulo");
            var farmB = Farm.Create(organization.Id, "Fazenda B", 320m, "Sorriso", "MT", now, null, "BR", "MT", null, null, -12.54m, -55.72m, "America/Cuiaba");
            var fieldA = Field.Create(organization.Id, farmA.Id, "A-01", 50m, now);
            var fieldB = Field.Create(organization.Id, farmB.Id, "B-01", 60m, now);
            var crop = Crop.Create(organization.Id, "Soja", "Teste", now);
            var seasonA = Season.Create(organization.Id, fieldA.Id, crop.Id, "Safra A", new DateOnly(2026, 9, 1), null, 60m, now);
            var seasonB = Season.Create(organization.Id, fieldB.Id, crop.Id, "Safra B", new DateOnly(2026, 9, 1), null, 62m, now);

            var category = InventoryCategory.Create(organization.Id, "Sementes", now);
            var item = InventoryItem.Create(organization.Id, category.Id, "SEM-SCOPE", "Semente escopo", UnitOfMeasure.Sack, 5m, now);
            var warehouseA = Warehouse.Create(organization.Id, farmA.Id, "Depósito A", null, now);
            var warehouseB = Warehouse.Create(organization.Id, farmB.Id, "Depósito B", null, now);
            var movementA = StockMovement.Create(organization.Id, item.Id, warehouseA.Id, StockMovementType.Entry, 10m, now, null, null, null, farmA.Id, fieldA.Id, seasonA.Id, now);
            var movementB = StockMovement.Create(organization.Id, item.Id, warehouseB.Id, StockMovementType.Entry, 20m, now, null, null, null, farmB.Id, fieldB.Id, seasonB.Id, now);

            var machineA = Machine.Create(organization.Id, farmA.Id, "Trator A", "SCOPE-A", MachineKind.Tractor, "Marca", "A1", 2026, 100m, now);
            var machineB = Machine.Create(organization.Id, farmB.Id, "Trator B", "SCOPE-B", MachineKind.Tractor, "Marca", "B1", 2026, 100m, now);

            var irrigationA = IrrigationZone.Create(organization.Id, fieldA.Id, "Zona A", 10m, IrrigationMethod.Drip, 30m, 50m, 75m, null, now);
            var irrigationB = IrrigationZone.Create(organization.Id, fieldB.Id, "Zona B", 10m, IrrigationMethod.Drip, 30m, 50m, 75m, null, now);

            var factor = EmissionFactor.Create(organization.Id, "Diesel escopo", EmissionSourceCategory.Fuel, "L", 2.5m, "Teste de escopo", null, new DateOnly(2026, 1, 1), null, now);
            var emissionA = EmissionActivity.Create(organization.Id, factor, 100m, new DateOnly(2026, 9, 8), EmissionActivityOrigin.Manual, SustainabilityDataQuality.Recorded, "Atividade A", farmA.Id, fieldA.Id, seasonA.Id, null, null, null, now);
            var emissionB = EmissionActivity.Create(organization.Id, factor, 100m, new DateOnly(2026, 9, 8), EmissionActivityOrigin.Manual, SustainabilityDataQuality.Recorded, "Atividade B", farmB.Id, fieldB.Id, seasonB.Id, null, null, null, now);

            seed.Organizations.Add(organization);
            seed.Farms.AddRange(farmA, farmB);
            seed.Fields.AddRange(fieldA, fieldB);
            seed.Crops.Add(crop);
            seed.Seasons.AddRange(seasonA, seasonB);
            seed.InventoryCategories.Add(category);
            seed.InventoryItems.Add(item);
            seed.Warehouses.AddRange(warehouseA, warehouseB);
            seed.StockMovements.AddRange(movementA, movementB);
            seed.Machines.AddRange(machineA, machineB);
            seed.IrrigationZones.AddRange(irrigationA, irrigationB);
            seed.EmissionFactors.Add(factor);
            seed.EmissionActivities.AddRange(emissionA, emissionB);
            await seed.SaveChangesAsync();

            var exportService = new ExportService(new ExportRepository(seed), new ProductionRepository(seed), new UnitOfWork(seed));
            var exportA = await exportService.CreateOrderAsync(organization.Id, new CreateExportOrderCommand(
                "EXP-SCOPE-A", "Comprador A", null, "US", farmA.Id, fieldA.Id, crop.Id, seasonA.Id,
                "Soja", 100m, "t", "USD", 500m, 5m, IncotermCode.FOB, "Santos/SP", "New Orleans/US",
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 20), null, null, null, null));
            var exportB = await exportService.CreateOrderAsync(organization.Id, new CreateExportOrderCommand(
                "EXP-SCOPE-B", "Comprador B", null, "US", farmB.Id, fieldB.Id, crop.Id, seasonB.Id,
                "Soja", 100m, "t", "USD", 500m, 5m, IncotermCode.FOB, "Santos/SP", "New Orleans/US",
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 20), null, null, null, null));
            Assert.True(exportA.Succeeded);
            Assert.True(exportB.Succeeded);

            var commercialService = new CommercialService(
                new CommercialRepository(seed), new ProductionRepository(seed), new ExportRepository(seed), new UnitOfWork(seed));
            var customer = await commercialService.CreateCustomerAsync(organization.Id, new CreateCommercialCustomerCommand(
                "Cliente Escopo", null, null, "cliente@scope.test", null, "BR", "Campinas", "SP", CustomerStatus.Prospect, null));
            Assert.True(customer.Succeeded);
            var opportunityA = await commercialService.CreateOpportunityAsync(organization.Id, new CreateCommercialOpportunityCommand(
                customer.Value!.Id, "Oportunidade A", farmA.Id, fieldA.Id, crop.Id, seasonA.Id, 100_000m, "BRL", 50m,
                new DateOnly(2026, 10, 31), "Equipe", "Próximo passo A", null));
            var opportunityB = await commercialService.CreateOpportunityAsync(organization.Id, new CreateCommercialOpportunityCommand(
                customer.Value.Id, "Oportunidade B", farmB.Id, fieldB.Id, crop.Id, seasonB.Id, 200_000m, "BRL", 50m,
                new DateOnly(2026, 10, 31), "Equipe", "Próximo passo B", null));
            Assert.True(opportunityA.Succeeded);
            Assert.True(opportunityB.Succeeded);

            organizationId = organization.Id;
            farmAId = farmA.Id;
            farmBId = farmB.Id;
            warehouseAId = warehouseA.Id;
            warehouseBId = warehouseB.Id;
            movementAId = movementA.Id;
            movementBId = movementB.Id;
            machineAId = machineA.Id;
            machineBId = machineB.Id;
            irrigationAId = irrigationA.Id;
            irrigationBId = irrigationB.Id;
            emissionAId = emissionA.Id;
            emissionBId = emissionB.Id;
            exportAId = exportA.Value!.Id;
            exportBId = exportB.Value!.Id;
            opportunityAId = opportunityA.Value!.Id;
            opportunityBId = opportunityB.Value!.Id;
        }

        var scope = new OperationalScopeContext();
        scope.Initialize(organizationId, Guid.NewGuid(), new FarmAccessScopeSnapshot(false, [farmAId], []));
        await using var scopedDb = new AgroControlDbContext(options, scope);

        Assert.Equal([farmAId], await scopedDb.Farms.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.DoesNotContain(farmBId, await scopedDb.Farms.AsNoTracking().Select(x => x.Id).ToListAsync());

        Assert.Equal([warehouseAId], await scopedDb.Warehouses.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.Warehouses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == warehouseBId));
        Assert.Equal([movementAId], await scopedDb.StockMovements.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.StockMovements.AsNoTracking().SingleOrDefaultAsync(x => x.Id == movementBId));

        Assert.Equal([machineAId], await scopedDb.Machines.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.Machines.AsNoTracking().SingleOrDefaultAsync(x => x.Id == machineBId));

        Assert.Equal([irrigationAId], await scopedDb.IrrigationZones.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.IrrigationZones.AsNoTracking().SingleOrDefaultAsync(x => x.Id == irrigationBId));

        Assert.Equal([emissionAId], await scopedDb.EmissionActivities.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.EmissionActivities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == emissionBId));

        Assert.Equal([exportAId], await scopedDb.ExportOrders.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.ExportOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == exportBId));
        Assert.DoesNotContain(await scopedDb.ExportDocuments.AsNoTracking().ToListAsync(), x => x.ExportOrderId == exportBId);

        Assert.Equal([opportunityAId], await scopedDb.CommercialOpportunities.AsNoTracking().Select(x => x.Id).ToListAsync());
        Assert.Null(await scopedDb.CommercialOpportunities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == opportunityBId));
        Assert.DoesNotContain(await scopedDb.OpportunityStageEvents.AsNoTracking().ToListAsync(), x => x.OpportunityId == opportunityBId);
    }
}
