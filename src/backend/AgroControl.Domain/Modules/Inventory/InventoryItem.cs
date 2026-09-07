namespace AgroControl.Domain.Modules.Inventory;

public sealed class InventoryItem
{
    private InventoryItem() { }

    private InventoryItem(
        Guid id,
        Guid organizationId,
        Guid? categoryId,
        string sku,
        string name,
        UnitOfMeasure unit,
        decimal minimumStock,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CategoryId = categoryId;
        Sku = sku;
        Name = name;
        Unit = unit;
        MinimumStock = minimumStock;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public UnitOfMeasure Unit { get; private set; }
    public decimal MinimumStock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static InventoryItem Create(
        Guid organizationId,
        Guid? categoryId,
        string sku,
        string name,
        UnitOfMeasure unit,
        decimal minimumStock,
        DateTime nowUtc)
    {
        Validate(organizationId, sku, name, minimumStock);
        return new InventoryItem(Guid.NewGuid(), organizationId, categoryId, NormalizeSku(sku), name.Trim(), unit, minimumStock, nowUtc);
    }

    public void Update(Guid? categoryId, string sku, string name, UnitOfMeasure unit, decimal minimumStock, DateTime nowUtc)
    {
        Validate(OrganizationId, sku, name, minimumStock);
        CategoryId = categoryId;
        Sku = NormalizeSku(sku);
        Name = name.Trim();
        Unit = unit;
        MinimumStock = minimumStock;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(Guid organizationId, string sku, string name, decimal minimumStock)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU is required.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Item name is required.", nameof(name));
        if (minimumStock < 0) throw new ArgumentOutOfRangeException(nameof(minimumStock), "Minimum stock cannot be negative.");
    }

    private static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();
}
