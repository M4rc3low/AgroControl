using AgroControl.Domain.Modules.Operations;

namespace AgroControl.Application.RegionalOperations;

public sealed record OperationalRegionDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateOperationalRegionCommand(string Name, string Code, string? Description);
public sealed record UpdateOperationalRegionCommand(string Name, string Code, string? Description);

public sealed record FarmAccessAssignmentDto(
    Guid Id,
    Guid UserId,
    FarmAccessScopeType ScopeType,
    Guid? TargetId,
    string ScopeKey,
    bool IsActive,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    Guid? RevokedByUserId,
    DateTime? RevokedAtUtc);

public sealed record GrantFarmAccessCommand(Guid UserId, FarmAccessScopeType ScopeType, Guid? TargetId);

public sealed record FarmAccessScopeSnapshot(
    bool AllFarms,
    IReadOnlyCollection<Guid> FarmIds,
    IReadOnlyCollection<Guid> RegionIds);

public sealed record FarmAccessScopeDto(
    Guid UserId,
    bool AllFarms,
    IReadOnlyCollection<Guid> FarmIds,
    IReadOnlyCollection<Guid> RegionIds);
