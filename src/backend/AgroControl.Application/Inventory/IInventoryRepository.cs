using AgroControl.Domain.Modules.Inventory;

namespace AgroControl.Application.Inventory;

public interface IInventoryRepository
{
    Task<(IReadOnlyList<InventoryCategory> Items, int TotalCount)> ListCategoriesAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<InventoryCategory?> GetCategoryAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> CategoryNameExistsAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddCategory(InventoryCategory category);

    Task<(IReadOnlyList<InventoryItem> Items, int TotalCount)> ListItemsAsync(Guid organizationId, int skip, int take, Guid? categoryId, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetItemAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> ItemSkuExistsAsync(Guid organizationId, string sku, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddItem(InventoryItem item);

    Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> ListWarehousesAsync(Guid organizationId, int skip, int take, Guid? farmId, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetWarehouseAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> WarehouseNameExistsAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddWarehouse(Warehouse warehouse);

    Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> ListMovementsAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? itemId,
        Guid? warehouseId,
        StockMovementType? type,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(Guid organizationId, Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryBalanceSnapshot>> ListLowStockAsync(Guid organizationId, int take, CancellationToken cancellationToken = default);
    void AddMovement(StockMovement movement);
}
