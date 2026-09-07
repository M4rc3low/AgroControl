namespace AgroControl.Domain.Modules.Finance;

public sealed class CostCenter
{
    private CostCenter() { }

    private CostCenter(Guid id, Guid organizationId, string name, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static CostCenter Create(Guid organizationId, string name, DateTime nowUtc)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Cost center name is required.", nameof(name));
        return new CostCenter(Guid.NewGuid(), organizationId, name.Trim(), nowUtc);
    }

    public void Update(string name, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Cost center name is required.", nameof(name));
        Name = name.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }
}
