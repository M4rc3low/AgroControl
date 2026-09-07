namespace AgroControl.Domain.Modules.Fields;

public sealed class Field
{
    private Field() { }

    private Field(
        Guid id,
        Guid organizationId,
        Guid farmId,
        string name,
        decimal areaHectares,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FarmId = farmId;
        Name = name;
        AreaHectares = areaHectares;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal AreaHectares { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Field Create(Guid organizationId, Guid farmId, string name, decimal areaHectares, DateTime nowUtc)
    {
        Validate(farmId, name, areaHectares);
        return new Field(Guid.NewGuid(), organizationId, farmId, name.Trim(), areaHectares, nowUtc);
    }

    public void Update(Guid farmId, string name, decimal areaHectares, DateTime nowUtc)
    {
        Validate(farmId, name, areaHectares);
        FarmId = farmId;
        Name = name.Trim();
        AreaHectares = areaHectares;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(Guid farmId, string name, decimal area)
    {
        if (farmId == Guid.Empty)
            throw new ArgumentException("Farm id is required.", nameof(farmId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Field name is required.", nameof(name));
        if (area <= 0)
            throw new ArgumentOutOfRangeException(nameof(area), "Field area must be greater than zero.");
    }
}
