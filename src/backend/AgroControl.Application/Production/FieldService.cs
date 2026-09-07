using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Fields;

namespace AgroControl.Application.Production;

public sealed class FieldService(IProductionRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<FieldDto>> ListAsync(Guid organizationId, int page, int pageSize, Guid? farmId, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await repository.ListFieldsAsync(organizationId, (page - 1) * pageSize, pageSize, farmId, search, includeInactive, cancellationToken);
        return new PagedResult<FieldDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<FieldDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var field = await repository.GetFieldAsync(organizationId, id, false, cancellationToken);
        return field is null ? null : ToDto(field);
    }

    public async Task<OperationResult<FieldDto>> CreateAsync(Guid organizationId, CreateFieldCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.AreaHectares <= 0)
            return OperationResult<FieldDto>.Validation("Name is required and areaHectares must be greater than zero.");

        var farm = await repository.GetFarmAsync(organizationId, command.FarmId, false, cancellationToken);
        if (farm is null || !farm.IsActive)
            return OperationResult<FieldDto>.Validation("Farm does not exist or is inactive for this organization.");

        var allocatedArea = await repository.GetAllocatedFieldAreaAsync(organizationId, command.FarmId, null, cancellationToken);
        if (allocatedArea + command.AreaHectares > farm.TotalAreaHectares)
            return OperationResult<FieldDto>.Validation("The sum of active field areas cannot be greater than the farm total area.");

        var field = Field.Create(organizationId, command.FarmId, command.Name, command.AreaHectares, DateTime.UtcNow);
        repository.AddField(field);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FieldDto>.Success(ToDto(field));
    }

    public async Task<OperationResult<FieldDto>> UpdateAsync(Guid organizationId, Guid id, UpdateFieldCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.AreaHectares <= 0)
            return OperationResult<FieldDto>.Validation("Name is required and areaHectares must be greater than zero.");

        var field = await repository.GetFieldAsync(organizationId, id, true, cancellationToken);
        if (field is null)
            return OperationResult<FieldDto>.NotFound("Field not found.");

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

    public async Task<OperationResult<bool>> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var field = await repository.GetFieldAsync(organizationId, id, true, cancellationToken);
        if (field is null)
            return OperationResult<bool>.NotFound("Field not found.");

        field.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static FieldDto ToDto(Field field) => new(field.Id, field.FarmId, field.Name, field.AreaHectares, field.IsActive, field.CreatedAtUtc, field.UpdatedAtUtc);
}
