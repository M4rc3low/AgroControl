namespace AgroControl.Domain.Modules.Identity;

public sealed class OrganizationMembership
{
    private OrganizationMembership() { }

    private OrganizationMembership(Guid organizationId, Guid userId, OrganizationRole role, DateTime createdAtUtc)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public OrganizationRole Role { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static OrganizationMembership Create(
        Guid organizationId,
        Guid userId,
        OrganizationRole role,
        DateTime createdAtUtc)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization id is required.", nameof(organizationId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        return new OrganizationMembership(organizationId, userId, role, createdAtUtc);
    }
}
