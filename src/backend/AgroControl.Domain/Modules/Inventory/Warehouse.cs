namespace AgroControl.Domain.Modules.Inventory;

public sealed class Warehouse
{
    private Warehouse() { }

    private Warehouse(Guid id, Guid organizationId, Guid? farmId, string name, string? location, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FarmId = farmId;
        Name = name;
        Location = location;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? FarmId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Warehouse Create(Guid organizationId, Guid? farmId, string name, string? location, DateTime nowUtc)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Warehouse name is required.", nameof(name));
        return new Warehouse(Guid.NewGuid(), organizationId, farmId, name.Trim(), NormalizeOptional(location), nowUtc);
    }

    public void Update(Guid? farmId, string name, string? location, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Warehouse name is required.", nameof(name));
        FarmId = farmId;
        Name = name.Trim();
        Location = NormalizeOptional(location);
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
