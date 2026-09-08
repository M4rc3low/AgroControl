namespace AgroControl.Application.RegionalOperations;

public interface IOperationalScopeContext
{
    bool IsInitialized { get; }
    bool IsRestricted { get; }
    Guid OrganizationId { get; }
    Guid UserId { get; }
    IReadOnlyList<Guid> FarmIds { get; }
    IReadOnlyList<Guid> RegionIds { get; }
}

public sealed class OperationalScopeContext : IOperationalScopeContext
{
    private Guid[] _farmIds = [];
    private Guid[] _regionIds = [];

    public bool IsInitialized { get; private set; }
    public bool IsRestricted { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public IReadOnlyList<Guid> FarmIds => _farmIds;
    public IReadOnlyList<Guid> RegionIds => _regionIds;

    public void Initialize(Guid organizationId, Guid userId, FarmAccessScopeSnapshot scope)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(scope);

        OrganizationId = organizationId;
        UserId = userId;
        _farmIds = scope.FarmIds.Distinct().ToArray();
        _regionIds = scope.RegionIds.Distinct().ToArray();
        IsRestricted = !scope.AllFarms;
        IsInitialized = true;
    }
}
