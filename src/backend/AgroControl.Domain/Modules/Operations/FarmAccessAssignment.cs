namespace AgroControl.Domain.Modules.Operations;

public enum FarmAccessScopeType
{
    AllFarms = 1,
    Region = 2,
    Farm = 3
}

public sealed class FarmAccessAssignment
{
    private FarmAccessAssignment() { }

    private FarmAccessAssignment(
        Guid id,
        Guid organizationId,
        Guid userId,
        FarmAccessScopeType scopeType,
        Guid? targetId,
        Guid createdByUserId,
        DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        ScopeType = scopeType;
        TargetId = targetId;
        ScopeKey = BuildScopeKey(scopeType, targetId);
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public FarmAccessScopeType ScopeType { get; private set; }
    public Guid? TargetId { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public static FarmAccessAssignment Create(
        Guid organizationId,
        Guid userId,
        FarmAccessScopeType scopeType,
        Guid? targetId,
        Guid createdByUserId,
        DateTime nowUtc)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Creator user id is required.", nameof(createdByUserId));
        _ = BuildScopeKey(scopeType, targetId);
        return new FarmAccessAssignment(Guid.NewGuid(), organizationId, userId, scopeType, targetId, createdByUserId, nowUtc);
    }

    public static FarmAccessAssignment CreateAllFarms(Guid organizationId, Guid userId, Guid createdByUserId, DateTime nowUtc) =>
        Create(organizationId, userId, FarmAccessScopeType.AllFarms, null, createdByUserId, nowUtc);

    public void Revoke(Guid revokedByUserId, DateTime nowUtc)
    {
        if (revokedByUserId == Guid.Empty) throw new ArgumentException("Revoker user id is required.", nameof(revokedByUserId));
        if (!IsActive) return;
        IsActive = false;
        RevokedByUserId = revokedByUserId;
        RevokedAtUtc = nowUtc;
    }

    public static string BuildScopeKey(FarmAccessScopeType scopeType, Guid? targetId) => scopeType switch
    {
        FarmAccessScopeType.AllFarms when targetId is null => "all",
        FarmAccessScopeType.Region when targetId is not null && targetId != Guid.Empty => $"region:{targetId.Value:N}",
        FarmAccessScopeType.Farm when targetId is not null && targetId != Guid.Empty => $"farm:{targetId.Value:N}",
        FarmAccessScopeType.AllFarms => throw new ArgumentException("AllFarms scope cannot have a target id.", nameof(targetId)),
        FarmAccessScopeType.Region => throw new ArgumentException("Region scope requires a target id.", nameof(targetId)),
        FarmAccessScopeType.Farm => throw new ArgumentException("Farm scope requires a target id.", nameof(targetId)),
        _ => throw new ArgumentOutOfRangeException(nameof(scopeType), "Unknown farm access scope type.")
    };
}
