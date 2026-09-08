using AgroControl.Domain.Modules.Operations;

namespace AgroControl.Application.RegionalOperations;

public interface IMultiFarmRepository
{
    Task<(IReadOnlyList<OperationalRegion> Items, int TotalCount)> ListRegionsAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<OperationalRegion?> GetRegionAsync(
        Guid organizationId,
        Guid regionId,
        bool tracking,
        CancellationToken cancellationToken = default);

    Task<bool> RegionCodeExistsAsync(
        Guid organizationId,
        string normalizedCode,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    void AddRegion(OperationalRegion region);

    Task<IReadOnlyList<FarmAccessAssignment>> ListAssignmentsAsync(
        Guid organizationId,
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<FarmAccessAssignment?> GetAssignmentAsync(
        Guid organizationId,
        Guid assignmentId,
        bool tracking,
        CancellationToken cancellationToken = default);

    Task<bool> AssignmentExistsAsync(
        Guid organizationId,
        Guid userId,
        string scopeKey,
        CancellationToken cancellationToken = default);

    Task<bool> UserBelongsToOrganizationAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListFarmIdsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListFarmIdsByRegionIdsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> regionIds,
        CancellationToken cancellationToken = default);

    Task<bool> FarmExistsAsync(
        Guid organizationId,
        Guid farmId,
        CancellationToken cancellationToken = default);

    void AddAssignment(FarmAccessAssignment assignment);
}
