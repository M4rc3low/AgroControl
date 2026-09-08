using AgroControl.Domain.Modules.Operations;

namespace AgroControl.Application.RegionalOperations;

public interface IFarmAccessScope
{
    Task<FarmAccessScopeSnapshot> GetEffectiveScopeAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessFarmAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default);
}

public sealed class FarmAccessScopeService(IMultiFarmRepository repository) : IFarmAccessScope
{
    public async Task<FarmAccessScopeSnapshot> GetEffectiveScopeAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await repository.ListAssignmentsAsync(organizationId, userId, false, cancellationToken);
        var allFarms = assignments.Any(item => item.ScopeType == FarmAccessScopeType.AllFarms);

        var regionIds = assignments
            .Where(item => item.ScopeType == FarmAccessScopeType.Region && item.TargetId is not null)
            .Select(item => item.TargetId!.Value)
            .Distinct()
            .ToArray();

        IReadOnlyList<Guid> farmIds;
        if (allFarms)
        {
            farmIds = await repository.ListFarmIdsAsync(organizationId, cancellationToken);
        }
        else
        {
            var directFarmIds = assignments
                .Where(item => item.ScopeType == FarmAccessScopeType.Farm && item.TargetId is not null)
                .Select(item => item.TargetId!.Value);

            var regionFarmIds = regionIds.Length == 0
                ? Array.Empty<Guid>()
                : await repository.ListFarmIdsByRegionIdsAsync(organizationId, regionIds, cancellationToken);

            farmIds = directFarmIds
                .Concat(regionFarmIds)
                .Distinct()
                .ToArray();
        }

        return new FarmAccessScopeSnapshot(allFarms, farmIds, regionIds);
    }

    public async Task<bool> CanAccessFarmAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        if (farmId == Guid.Empty) return false;
        var scope = await GetEffectiveScopeAsync(organizationId, userId, cancellationToken);
        return scope.AllFarms || scope.FarmIds.Contains(farmId);
    }
}
