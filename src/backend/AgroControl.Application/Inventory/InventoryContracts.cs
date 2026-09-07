using AgroControl.Domain.Modules.Inventory;

namespace AgroControl.Application.Inventory;

public sealed record CreateInventoryCategoryCommand(string Name);
public sealed record UpdateInventoryCategoryCommand(string Name);
public sealed record InventoryCategoryDto(Guid Id, string Name, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateInventoryItemCommand(Guid? CategoryId, string Sku, string Name, UnitOfMeasure Unit, decimal MinimumStock);
public sealed record UpdateInventoryItemCommand(Guid? CategoryId, string Sku, string Name, UnitOfMeasure Unit, decimal MinimumStock);
public sealed record InventoryItemDto(Guid Id, Guid? CategoryId, string Sku, string Name, UnitOfMeasure Unit, decimal MinimumStock, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateWarehouseCommand(Guid? FarmId, string Name, string? Location);
public sealed record UpdateWarehouseCommand(Guid? FarmId, string Name, string? Location);
public sealed record WarehouseDto(Guid Id, Guid? FarmId, string Name, string? Location, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateStockMovementCommand(
    Guid ItemId,
    Guid WarehouseId,
    StockMovementType Type,
    decimal Quantity,
    DateTime? OccurredAtUtc,
    string? BatchNumber,
    DateOnly? ExpirationDate,
    string? Notes,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId);

public sealed record StockMovementDto(
    Guid Id,
    Guid ItemId,
    Guid WarehouseId,
    StockMovementType Type,
    decimal Quantity,
    decimal SignedQuantity,
    DateTime OccurredAtUtc,
    string? BatchNumber,
    DateOnly? ExpirationDate,
    string? Notes,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    DateTime CreatedAtUtc);

public sealed record StockMovementCreatedDto(StockMovementDto Movement, decimal BalanceAfter);
public sealed record StockBalanceDto(Guid ItemId, Guid WarehouseId, decimal Balance);
public sealed record LowStockItemDto(Guid ItemId, string Sku, string Name, UnitOfMeasure Unit, decimal MinimumStock, decimal CurrentStock, decimal Shortage);
public sealed record InventoryBalanceSnapshot(Guid ItemId, string Sku, string Name, UnitOfMeasure Unit, decimal MinimumStock, decimal CurrentStock);
