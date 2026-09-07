namespace AgroControl.Domain.Modules.Farms;

public sealed class Farm
{
    private Farm() { }

    private Farm(
        Guid id,
        Guid organizationId,
        string name,
        decimal totalAreaHectares,
        string? city,
        string? state,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        TotalAreaHectares = totalAreaHectares;
        City = city;
        State = state;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TotalAreaHectares { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Farm Create(
        Guid organizationId,
        string name,
        decimal totalAreaHectares,
        string? city,
        string? state,
        DateTime nowUtc)
    {
        Validate(name, totalAreaHectares);
        return new Farm(
            Guid.NewGuid(),
            organizationId,
            name.Trim(),
            totalAreaHectares,
            Normalize(city),
            Normalize(state),
            nowUtc);
    }

    public void Update(string name, decimal totalAreaHectares, string? city, string? state, DateTime nowUtc)
    {
        Validate(name, totalAreaHectares);
        Name = name.Trim();
        TotalAreaHectares = totalAreaHectares;
        City = Normalize(city);
        State = Normalize(state);
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(string name, decimal area)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Farm name is required.", nameof(name));
        if (area <= 0)
            throw new ArgumentOutOfRangeException(nameof(area), "Farm area must be greater than zero.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
