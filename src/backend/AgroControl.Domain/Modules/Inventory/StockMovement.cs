namespace AgroControl.Domain.Modules.Inventory;

public sealed class StockMovement
{
    private StockMovement() { }

    private StockMovement(
        Guid id,
        Guid organizationId,
        Guid itemId,
        Guid warehouseId,
        StockMovementType type,
        decimal quantity,
        DateTime occurredAtUtc,
        string? batchNumber,
        DateOnly? expirationDate,
        string? notes,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        Type = type;
        Quantity = quantity;
        OccurredAtUtc = occurredAtUtc;
        BatchNumber = NormalizeOptional(batchNumber);
        ExpirationDate = expirationDate;
        Notes = NormalizeOptional(notes);
        FarmId = farmId;
        FieldId = fieldId;
        SeasonId = seasonId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }
    public string? Notes { get; private set; }
    public Guid? FarmId { get; private set; }
    public Guid? FieldId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public decimal SignedQuantity => Type is StockMovementType.Entry or StockMovementType.AdjustmentPositive ? Quantity : -Quantity;

    public static StockMovement Create(
        Guid organizationId,
        Guid itemId,
        Guid warehouseId,
        StockMovementType type,
        decimal quantity,
        DateTime occurredAtUtc,
        string? batchNumber,
        DateOnly? expirationDate,
        string? notes,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        DateTime nowUtc)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (itemId == Guid.Empty) throw new ArgumentException("Item id is required.", nameof(itemId));
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse id is required.", nameof(warehouseId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Movement quantity must be greater than zero.");
        if (expirationDate is not null && string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("A batch number is required when expiration date is informed.", nameof(batchNumber));

        return new StockMovement(
            Guid.NewGuid(), organizationId, itemId, warehouseId, type, quantity, occurredAtUtc,
            batchNumber, expirationDate, notes, farmId, fieldId, seasonId, nowUtc);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
