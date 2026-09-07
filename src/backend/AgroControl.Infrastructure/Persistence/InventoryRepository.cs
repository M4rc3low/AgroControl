using AgroControl.Application.Inventory;
using AgroControl.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class InventoryRepository(AgroControlDbContext dbContext) : IInventoryRepository
{
    public async Task<(IReadOnlyList<InventoryCategory> Items, int TotalCount)> ListCategoriesAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryCategories.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<InventoryCategory?> GetCategoryAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<InventoryCategory> query = dbContext.InventoryCategories.Where(x => x.OrganizationId == organizationId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CategoryNameExistsAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.InventoryCategories.Where(x => x.OrganizationId == organizationId && x.Name.ToLower() == normalized);
        if (excludingId is not null) query = query.Where(x => x.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void AddCategory(InventoryCategory category) => dbContext.InventoryCategories.Add(category);

    public async Task<(IReadOnlyList<InventoryItem> Items, int TotalCount)> ListItemsAsync(Guid organizationId, int skip, int take, Guid? categoryId, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryItems.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Sku.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Sku).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<InventoryItem?> GetItemAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<InventoryItem> query = dbContext.InventoryItems.Where(x => x.OrganizationId == organizationId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ItemSkuExistsAsync(Guid organizationId, string sku, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = sku.Trim().ToUpper();
        var query = dbContext.InventoryItems.Where(x => x.OrganizationId == organizationId && x.Sku == normalized);
        if (excludingId is not null) query = query.Where(x => x.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void AddItem(InventoryItem item) => dbContext.InventoryItems.Add(item);

    public async Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> ListWarehousesAsync(Guid organizationId, int skip, int take, Guid? farmId, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Warehouses.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (farmId is not null) query = query.Where(x => x.FarmId == farmId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || (x.Location != null && x.Location.ToLower().Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Warehouse?> GetWarehouseAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Warehouse> query = dbContext.Warehouses.Where(x => x.OrganizationId == organizationId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> WarehouseNameExistsAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.Warehouses.Where(x => x.OrganizationId == organizationId && x.Name.ToLower() == normalized);
        if (excludingId is not null) query = query.Where(x => x.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public void AddWarehouse(Warehouse warehouse) => dbContext.Warehouses.Add(warehouse);

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(Guid organizationId, int skip, int take, Guid? itemId, Guid? warehouseId, StockMovementType? type, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockMovements.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (itemId is not null) query = query.Where(x => x.ItemId == itemId);
        if (warehouseId is not null) query = query.Where(x => x.WarehouseId == warehouseId);
        if (type is not null) query = query.Where(x => x.Type == type);
        if (fromUtc is not null) query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
        if (toUtc is not null) query = query.Where(x => x.OccurredAtUtc <= toUtc.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<decimal> GetBalanceAsync(Guid organizationId, Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await dbContext.StockMovements
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ItemId == itemId && x.WarehouseId == warehouseId)
            .SumAsync(x => (decimal?)(x.Type == StockMovementType.Entry || x.Type == StockMovementType.AdjustmentPositive ? x.Quantity : -x.Quantity), cancellationToken) ?? 0m;
    }

    public async Task<IReadOnlyList<InventoryBalanceSnapshot>> ListLowStockAsync(Guid organizationId, int take, CancellationToken cancellationToken = default)
    {
        var balances = await dbContext.StockMovements
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .GroupBy(x => x.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                Balance = group.Sum(x => x.Type == StockMovementType.Entry || x.Type == StockMovementType.AdjustmentPositive ? x.Quantity : -x.Quantity)
            })
            .ToDictionaryAsync(x => x.ItemId, x => x.Balance, cancellationToken);

        var items = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive && x.MinimumStock > 0)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new InventoryBalanceSnapshot(item.Id, item.Sku, item.Name, item.Unit, item.MinimumStock, balances.GetValueOrDefault(item.Id, 0m)))
            .Where(item => item.CurrentStock < item.MinimumStock)
            .OrderByDescending(item => item.MinimumStock - item.CurrentStock)
            .Take(take)
            .ToList();
    }

    public void AddMovement(StockMovement movement) => dbContext.StockMovements.Add(movement);
}
