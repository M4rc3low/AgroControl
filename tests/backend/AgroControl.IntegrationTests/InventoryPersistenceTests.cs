using AgroControl.Application.Common;
using AgroControl.Application.Inventory;
using AgroControl.Domain.Modules.Inventory;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class InventoryPersistenceTests
{
    [Fact]
    public async Task Inventory_repository_isolates_categories_by_organization()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Grupo A", $"grupo-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Grupo B", $"grupo-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(organizationA, organizationB);
        dbContext.InventoryCategories.Add(InventoryCategory.Create(organizationA.Id, "Sementes", now));
        dbContext.InventoryCategories.Add(InventoryCategory.Create(organizationB.Id, "Defensivos", now));
        await dbContext.SaveChangesAsync();

        var repository = new InventoryRepository(dbContext);
        var (items, totalCount) = await repository.ListCategoriesAsync(
            organizationA.Id,
            skip: 0,
            take: 20,
            search: null,
            includeInactive: false);

        Assert.Equal(1, totalCount);
        var category = Assert.Single(items);
        Assert.Equal("Sementes", category.Name);
        Assert.Equal(organizationA.Id, category.OrganizationId);
    }

    [Fact]
    public async Task Inventory_ledger_calculates_balance_and_rejects_negative_stock()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organization = Organization.Create("Grupo Estoque", $"estoque-{Guid.NewGuid():N}", now);
        var category = InventoryCategory.Create(organization.Id, "Sementes", now);
        var item = InventoryItem.Create(organization.Id, category.Id, "SEM-001", "Semente de soja", UnitOfMeasure.Sack, 5m, now);
        var warehouse = Warehouse.Create(organization.Id, null, "Depósito Central", null, now);

        dbContext.Organizations.Add(organization);
        dbContext.InventoryCategories.Add(category);
        dbContext.InventoryItems.Add(item);
        dbContext.Warehouses.Add(warehouse);
        await dbContext.SaveChangesAsync();

        var inventoryRepository = new InventoryRepository(dbContext);
        var service = new InventoryService(
            inventoryRepository,
            new ProductionRepository(dbContext),
            new UnitOfWork(dbContext));

        var entry = await service.CreateMovementAsync(
            organization.Id,
            new CreateStockMovementCommand(
                item.Id,
                warehouse.Id,
                StockMovementType.Entry,
                10m,
                now,
                "LOTE-2026-01",
                DateOnly.FromDateTime(now.AddMonths(8)),
                "Compra inicial",
                null,
                null,
                null));

        Assert.True(entry.Succeeded);
        Assert.Equal(10m, entry.Value!.BalanceAfter);

        var exit = await service.CreateMovementAsync(
            organization.Id,
            new CreateStockMovementCommand(
                item.Id,
                warehouse.Id,
                StockMovementType.Exit,
                4m,
                now.AddMinutes(1),
                null,
                null,
                "Uso em campo",
                null,
                null,
                null));

        Assert.True(exit.Succeeded);
        Assert.Equal(6m, exit.Value!.BalanceAfter);
        Assert.Equal(6m, await inventoryRepository.GetBalanceAsync(organization.Id, item.Id, warehouse.Id));

        var excessiveExit = await service.CreateMovementAsync(
            organization.Id,
            new CreateStockMovementCommand(
                item.Id,
                warehouse.Id,
                StockMovementType.Exit,
                7m,
                now.AddMinutes(2),
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.False(excessiveExit.Succeeded);
        Assert.Equal(OperationErrorKind.Conflict, excessiveExit.ErrorKind);
        Assert.Equal(6m, await inventoryRepository.GetBalanceAsync(organization.Id, item.Id, warehouse.Id));
    }
}
