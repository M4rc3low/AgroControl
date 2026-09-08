namespace AgroControl.Domain.Modules.PrecisionAgriculture;

public enum ManagementZoneType
{
    Soil,
    Yield,
    Vegetation,
    Prescription,
    Custom
}

public sealed class ManagementZone
{
    private ManagementZone() { }

    private ManagementZone(Guid id, Guid organizationId, Guid fieldId, ManagementZoneType type, string name,
        string? description, string? classification, decimal? value, string? unit, DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FieldId = fieldId;
        Type = type;
        Name = name;
        Description = description;
        Classification = classification;
        Value = value;
        Unit = unit;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FieldId { get; private set; }
    public ManagementZoneType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Classification { get; private set; }
    public decimal? Value { get; private set; }
    public string? Unit { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static ManagementZone Create(Guid organizationId, Guid fieldId, ManagementZoneType type, string name,
        string? description, string? classification, decimal? value, string? unit, DateTime nowUtc)
    {
        Validate(organizationId, fieldId, type, name, description, classification, unit);
        return new ManagementZone(Guid.NewGuid(), organizationId, fieldId, type, name.Trim(), Clean(description),
            Clean(classification), value, Clean(unit), nowUtc);
    }

    public void Update(ManagementZoneType type, string name, string? description, string? classification,
        decimal? value, string? unit, DateTime nowUtc)
    {
        Validate(OrganizationId, FieldId, type, name, description, classification, unit);
        Type = type;
        Name = name.Trim();
        Description = Clean(description);
        Classification = Clean(classification);
        Value = value;
        Unit = Clean(unit);
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(Guid organizationId, Guid fieldId, ManagementZoneType type, string name,
        string? description, string? classification, string? unit)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (fieldId == Guid.Empty) throw new ArgumentException("Field id is required.", nameof(fieldId));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Zone name is required.", nameof(name));
        if (name.Trim().Length > 160) throw new ArgumentException("Zone name cannot exceed 160 characters.", nameof(name));
        if (description?.Trim().Length > 1000) throw new ArgumentException("Description cannot exceed 1000 characters.", nameof(description));
        if (classification?.Trim().Length > 120) throw new ArgumentException("Classification cannot exceed 120 characters.", nameof(classification));
        if (unit?.Trim().Length > 32) throw new ArgumentException("Unit cannot exceed 32 characters.", nameof(unit));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
