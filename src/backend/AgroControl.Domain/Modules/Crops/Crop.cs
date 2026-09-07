namespace AgroControl.Domain.Modules.Crops;

public sealed class Crop
{
    private Crop() { }

    private Crop(Guid id, Guid organizationId, string name, string? variety, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        Variety = variety;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Variety { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Crop Create(Guid organizationId, string name, string? variety, DateTime nowUtc)
    {
        Validate(name);
        return new Crop(Guid.NewGuid(), organizationId, name.Trim(), Normalize(variety), nowUtc);
    }

    public void Update(string name, string? variety, DateTime nowUtc)
    {
        Validate(name);
        Name = name.Trim();
        Variety = Normalize(variety);
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Crop name is required.", nameof(name));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
