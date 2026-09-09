using AgroControl.Application.Common;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Fields;

namespace AgroControl.Application.Production;

public sealed class FieldService(
    IProductionRepository repository,
    IFarmAccessScope accessScope,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<FieldDto>> ListAsync(
        Guid organizationId,
        Guid userId,
        int page,
        int pageSize,
        Guid? farmId,
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var scope = await accessScope.GetEffectiveScopeAsync(organizationId, userId, cancellationToken);
        if (farmId is not null && !scope.AllFarms && !scope.FarmIds.Contains(farmId.Value))
            return new PagedResult<FieldDto>(Array.Empty<FieldDto>(), page, pageSize, 0);

        IReadOnlyCollection<Guid>? allowedFarmIds = scope.AllFarms ? null : scope.FarmIds;
        var (items, totalCount) = await repository.ListFieldsAsync(
            organizationId,
            (page - 1) * pageSize,
            pageSize,
            farmId,
            search,
            includeInactive,
            allowedFarmIds,
            cancellationToken);
        return new PagedResult<FieldDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<FieldDto?> GetAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var field = await repository.GetFieldAsync(organizationId, id, false, cancellationToken);
        if (field is null || !await accessScope.CanAccessFarmAsync(organizationId, userId, field.FarmId, cancellationToken))
            return null;
        return ToDto(field);
    }

    public Task<OperationResult<FieldDto>> CreateAsync(
        Guid organizationId,
        Guid userId,
        CreateFieldCommand command,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(organizationId, userId, null, command, cancellationToken);

    public Task<OperationResult<FieldDto>> CreateWithIdAsync(
        Guid organizationId,
        Guid userId,
        Guid id,
        CreateFieldCommand command,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(organizationId, userId, id, command, cancellationToken);

    private async Task<OperationResult<FieldDto>> CreateCoreAsync(
        Guid organizationId,
        Guid userId,
        Guid? explicitId,
        CreateFieldCommand command,
        CancellationToken cancellationToken)
    {
        if (explicitId == Guid.Empty)
            return OperationResult<FieldDto>.Validation("Field id is required.");
        if (string.IsNullOrWhiteSpace(command.Name) || command.AreaHectares <= 0)
            return OperationResult<FieldDto>.Validation("Name is required and areaHectares must be greater than zero.");

        if (explicitId is not null &&
            await repository.GetFieldAsync(organizationId, explicitId.Value, false, cancellationToken) is not null)
            return OperationResult<FieldDto>.Conflict("Field id already exists.");

        if (!await accessScope.CanAccessFarmAsync(organizationId, userId, command.FarmId, cancellationToken))
            return OperationResult<FieldDto>.NotFound("Farm not found.");

        var farm = await repository.GetFarmAsync(organizationId, command.FarmId, false, cancellationToken);
        if (farm is null || !farm.IsActive)
            return OperationResult<FieldDto>.Validation("Farm does not exist or is inactive for this organization.");

        var allocatedArea = await repository.GetAllocatedFieldAreaAsync(organizationId, command.FarmId, null, cancellationToken);
        if (allocatedArea + command.AreaHectares > farm.TotalAreaHectares)
            return OperationResult<FieldDto>.Validation("The sum of active field areas cannot be greater than the farm total area.");

        var nowUtc = DateTime.UtcNow;
        var field = explicitId is null
            ? Field.Create(organizationId, command.FarmId, command.Name, command.AreaHectares, nowUtc)
            : Field.CreateWithId(explicitId.Value, organizationId, command.FarmId, command.Name, command.AreaHectares, nowUtc);
        repository.AddField(field);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FieldDto>.Success(ToDto(field));
    }

    public async Task<OperationResult<FieldDto>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        Guid id,
        UpdateFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.AreaHectares <= 0)
            return OperationResult<FieldDto>.Validation("Name is required and areaHectares must be greater than zero.");

        var field = await repository.GetFieldAsync(organizationId, id, true, cancellationToken);
        if (field is null || !await accessScope.CanAccessFarmAsync(organizationId, userId, field.FarmId, cancellationToken))
            return OperationResult<FieldDto>.NotFound("Field not found.");

        if (!await accessScope.CanAccessFarmAsync(organizationId, userId, command.FarmId, cancellationToken))
            return OperationResult<FieldDto>.NotFound("Target farm not found.");

        var farm = await repository.GetFarmAsync(organizationId, command.FarmId, false, cancellationToken);
        if (farm is null || !farm.IsActive)
            return OperationResult<FieldDto>.Validation("Farm does not exist or is inactive for this organization.");

        var allocatedArea = await repository.GetAllocatedFieldAreaAsync(organizationId, command.FarmId, id, cancellationToken);
        if (allocatedArea + command.AreaHectares > farm.TotalAreaHectares)
            return OperationResult<FieldDto>.Validation("The sum of active field areas cannot be greater than the farm total area.");

        field.Update(command.FarmId, command.Name, command.AreaHectares, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FieldDto>.Success(ToDto(field));
    }

    public async Task<OperationResult<bool>> DeactivateAsync(
        Guid organizationId,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var field = await repository.GetFieldAsync(organizationId, id, true, cancellationToken);
        if (field is null || !await accessScope.CanAccessFarmAsync(organizationId, userId, field.FarmId, cancellationToken))
            return OperationResult<bool>.NotFound("Field not found.");

        field.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static FieldDto ToDto(Field field) => new(
        field.Id,
        field.FarmId,
        field.Name,
        field.AreaHectares,
        field.IsActive,
        field.CreatedAtUtc,
        field.UpdatedAtUtc);
}
