using AgroControl.Application.Exporting;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Exporting;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class ExportPersistenceTests
{
    [Fact]
    public async Task Export_is_tenant_isolated_and_keeps_currency_cost_and_production_context()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var orgA = Organization.Create("Export A", $"export-a-{Guid.NewGuid():N}", now);
        var orgB = Organization.Create("Export B", $"export-b-{Guid.NewGuid():N}", now);
        var farm = Farm.Create(orgA.Id, "Fazenda A", 100m, "Rio Verde", "GO", now);
        var field = Field.Create(orgA.Id, farm.Id, "Talhão 01", 50m, now);
        var crop = Crop.Create(orgA.Id, "Soja", "Teste", now);
        var season = Season.Create(orgA.Id, field.Id, crop.Id, "Safra 2026/27", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), 60m, now);
        dbContext.Organizations.AddRange(orgA, orgB);
        dbContext.Farms.Add(farm);
        dbContext.Fields.Add(field);
        dbContext.Crops.Add(crop);
        dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync();

        var repository = new ExportRepository(dbContext);
        var service = new ExportService(repository, new ProductionRepository(dbContext), new UnitOfWork(dbContext));
        var created = await service.CreateOrderAsync(orgA.Id, new CreateExportOrderCommand(
            "EXP-2026-001", "Importadora Teste", "PO-123", "US", farm.Id, field.Id, crop.Id, season.Id,
            "Soja em grãos", 100m, "t", "USD", 500m, 5.2m, IncotermCode.FOB, "Santos/SP", "New Orleans/US",
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 20), null, null, null, null));
        Assert.True(created.Succeeded);
        Assert.Equal(260_000m, created.Value!.EstimatedValueBrl);

        var listA = await service.ListOrdersAsync(orgA.Id, 1, 20, null, null, null, null, null, null);
        var listB = await service.ListOrdersAsync(orgB.Id, 1, 20, null, null, null, null, null, null);
        Assert.Single(listA.Items);
        Assert.Empty(listB.Items);

        var docs = await service.ListDocumentsAsync(orgA.Id, created.Value.Id);
        Assert.True(docs.Succeeded);
        Assert.Equal(5, docs.Value!.Count);

        var contracted = await service.TransitionStatusAsync(orgA.Id, created.Value.Id,
            new TransitionExportOrderStatusCommand(ExportOrderStatus.Contracted, new DateOnly(2026, 9, 15), "Contrato assinado"));
        Assert.True(contracted.Succeeded);
        var invalid = await service.TransitionStatusAsync(orgA.Id, created.Value.Id,
            new TransitionExportOrderStatusCommand(ExportOrderStatus.Delivered, new DateOnly(2026, 9, 16), null));
        Assert.False(invalid.Succeeded);

        var cost = await service.AddCostAsync(orgA.Id, created.Value.Id, new CreateExportCostCommand(
            ExportCostType.Freight, "Frete marítimo", 10_000m, "USD", 5.1m, new DateOnly(2026, 9, 20), null));
        Assert.True(cost.Succeeded);
        Assert.Equal(51_000m, cost.Value!.AmountBrl);

        var summary = await service.GetSummaryAsync(orgA.Id, null, null, null, null, null);
        Assert.Equal(1, summary.OrderCount);
        Assert.Equal(260_000m, summary.EstimatedCommercialValueBrl);
        Assert.Equal(51_000m, summary.TotalLogisticsCostBrl);
        Assert.Equal(209_000m, summary.EstimatedOperationalMarginBrl);
    }
}
