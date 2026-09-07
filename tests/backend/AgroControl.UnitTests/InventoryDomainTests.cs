using AgroControl.Domain.Modules.Inventory;

namespace AgroControl.UnitTests;

public sealed class InventoryDomainTests
{
    [Fact]
    public void Inventory_item_rejects_negative_minimum_stock()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InventoryItem.Create(
            Guid.NewGuid(), null, "SEM-001", "Semente de soja", UnitOfMeasure.Sack, -1m, DateTime.UtcNow));
    }

    [Fact]
    public void Stock_movement_rejects_zero_quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StockMovement.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StockMovementType.Entry, 0m,
            DateTime.UtcNow, null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Stock_movement_sign_reflects_entry_and_exit()
    {
        var organizationId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entry = StockMovement.Create(organizationId, itemId, warehouseId, StockMovementType.Entry, 10m, now, null, null, null, null, null, null, now);
        var exit = StockMovement.Create(organizationId, itemId, warehouseId, StockMovementType.Exit, 3m, now, null, null, null, null, null, null, now);

        Assert.Equal(10m, entry.SignedQuantity);
        Assert.Equal(-3m, exit.SignedQuantity);
    }

    [Fact]
    public void Expiration_date_requires_batch_number()
    {
        Assert.Throws<ArgumentException>(() => StockMovement.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StockMovementType.Entry, 5m,
            DateTime.UtcNow, null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), null,
            null, null, null, DateTime.UtcNow));
    }
}
