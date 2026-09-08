namespace AgroControl.Domain.Modules.Operations;

public sealed class OperationalRegion
{
    private OperationalRegion() { }

    private OperationalRegion(
        Guid id,
        Guid organizationId,
        string name,
        string code,
        string? description,
        DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Apply(name, code, description, nowUtc);
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static OperationalRegion Create(
        Guid organizationId,
        string name,
        string code,
        string? description,
        DateTime nowUtc)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization id is required.", nameof(organizationId));

        return new OperationalRegion(Guid.NewGuid(), organizationId, name, code, description, nowUtc);
    }

    public void Update(string name, string code, string? description, DateTime nowUtc) =>
        Apply(name, code, description, nowUtc);

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private void Apply(string name, string code, string? description, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Region name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Region code is required.", nameof(code));

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length > 32 || normalizedCode.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Region code must contain only letters, numbers, '-' or '_' and have at most 32 characters.", nameof(code));

        Name = name.Trim();
        Code = normalizedCode;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAtUtc = nowUtc;
    }
}
