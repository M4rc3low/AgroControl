using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Operations;

namespace AgroControl.Application.RegionalOperations;

public sealed class MultiFarmService(
    IMultiFarmRepository repository,
    IFarmAccessScope accessScope,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<OperationalRegionDto>> ListRegionsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await repository.ListRegionsAsync(
            organizationId,
            (page - 1) * pageSize,
            pageSize,
            search,
            includeInactive,
            cancellationToken);

        return new PagedResult<OperationalRegionDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<OperationResult<OperationalRegionDto>> CreateRegionAsync(
        Guid organizationId,
        CreateOperationalRegionCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = command.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await repository.RegionCodeExistsAsync(organizationId, normalizedCode, null, cancellationToken))
            return OperationResult<OperationalRegionDto>.Conflict("A region with this code already exists.");

        try
        {
            var region = OperationalRegion.Create(
                organizationId,
                command.Name,
                normalizedCode,
                command.Description,
                DateTime.UtcNow);
            repository.AddRegion(region);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<OperationalRegionDto>.Success(ToDto(region));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<OperationalRegionDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<OperationalRegionDto>> UpdateRegionAsync(
        Guid organizationId,
        Guid regionId,
        UpdateOperationalRegionCommand command,
        CancellationToken cancellationToken = default)
    {
        var region = await repository.GetRegionAsync(organizationId, regionId, true, cancellationToken);
        if (region is null) return OperationResult<OperationalRegionDto>.NotFound("Region not found.");

        var normalizedCode = command.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await repository.RegionCodeExistsAsync(organizationId, normalizedCode, regionId, cancellationToken))
            return OperationResult<OperationalRegionDto>.Conflict("A region with this code already exists.");

        try
        {
            region.Update(command.Name, normalizedCode, command.Description, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<OperationalRegionDto>.Success(ToDto(region));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<OperationalRegionDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> DeactivateRegionAsync(
        Guid organizationId,
        Guid regionId,
        CancellationToken cancellationToken = default)
    {
        var region = await repository.GetRegionAsync(organizationId, regionId, true, cancellationToken);
        if (region is null) return OperationResult<bool>.NotFound("Region not found.");
        region.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<FarmAccessScopeDto> GetEffectiveScopeAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var scope = await accessScope.GetEffectiveScopeAsync(organizationId, userId, cancellationToken);
        return new FarmAccessScopeDto(userId, scope.AllFarms, scope.FarmIds, scope.RegionIds);
    }

    public async Task<IReadOnlyList<FarmAccessAssignmentDto>> ListAssignmentsAsync(
        Guid organizationId,
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.ListAssignmentsAsync(organizationId, userId, includeInactive, cancellationToken);
        return items.OrderByDescending(item => item.CreatedAtUtc).Select(ToDto).ToList();
    }

    public async Task<OperationResult<FarmAccessAssignmentDto>> GrantAccessAsync(
        Guid organizationId,
        Guid actorUserId,
        GrantFarmAccessCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.UserBelongsToOrganizationAsync(organizationId, command.UserId, cancellationToken))
            return OperationResult<FarmAccessAssignmentDto>.NotFound("User not found in this organization.");

        try
        {
            if (command.ScopeType == FarmAccessScopeType.Region)
            {
                var region = command.TargetId is null
                    ? null
                    : await repository.GetRegionAsync(organizationId, command.TargetId.Value, false, cancellationToken);
                if (region is null || !region.IsActive)
                    return OperationResult<FarmAccessAssignmentDto>.Validation("Region does not exist or is inactive for this organization.");
            }
            else if (command.ScopeType == FarmAccessScopeType.Farm)
            {
                if (command.TargetId is null || !await repository.FarmExistsAsync(organizationId, command.TargetId.Value, cancellationToken))
                    return OperationResult<FarmAccessAssignmentDto>.Validation("Farm does not exist for this organization.");
            }

            var scopeKey = FarmAccessAssignment.BuildScopeKey(command.ScopeType, command.TargetId);
            if (await repository.AssignmentExistsAsync(organizationId, command.UserId, scopeKey, cancellationToken))
                return OperationResult<FarmAccessAssignmentDto>.Conflict("This access scope is already active for the user.");

            var assignment = FarmAccessAssignment.Create(
                organizationId,
                command.UserId,
                command.ScopeType,
                command.TargetId,
                actorUserId,
                DateTime.UtcNow);
            repository.AddAssignment(assignment);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<FarmAccessAssignmentDto>.Success(ToDto(assignment));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<FarmAccessAssignmentDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> RevokeAccessAsync(
        Guid organizationId,
        Guid actorUserId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await repository.GetAssignmentAsync(organizationId, assignmentId, true, cancellationToken);
        if (assignment is null) return OperationResult<bool>.NotFound("Access assignment not found.");
        assignment.Revoke(actorUserId, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static OperationalRegionDto ToDto(OperationalRegion region) => new(
        region.Id,
        region.Name,
        region.Code,
        region.Description,
        region.IsActive,
        region.CreatedAtUtc,
        region.UpdatedAtUtc);

    private static FarmAccessAssignmentDto ToDto(FarmAccessAssignment assignment) => new(
        assignment.Id,
        assignment.UserId,
        assignment.ScopeType,
        assignment.TargetId,
        assignment.ScopeKey,
        assignment.IsActive,
        assignment.CreatedByUserId,
        assignment.CreatedAtUtc,
        assignment.RevokedByUserId,
        assignment.RevokedAtUtc);
}
