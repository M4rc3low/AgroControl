using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Farms;

namespace AgroControl.Application.Production;

public sealed class FarmService(IProductionRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<FarmDto>> ListAsync(Guid organizationId, int page, int pageSize, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, totalCount) = await repository.ListFarmsAsync(organizationId, (page - 1) * pageSize, pageSize, search, includeInactive, cancellationToken);
        return new PagedResult<FarmDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<FarmDto?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var farm = await repository.GetFarmAsync(organizationId, id, false, cancellationToken);
        return farm is null ? null : ToDto(farm);
    }

    public async Task<OperationResult<FarmDto>> CreateAsync(Guid organizationId, CreateFarmCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.TotalAreaHectares <= 0)
            return OperationResult<FarmDto>.Validation("Name is required and totalAreaHectares must be greater than zero.");

        var farm = Farm.Create(organizationId, command.Name, command.TotalAreaHectares, command.City, command.State, DateTime.UtcNow);
        repository.AddFarm(farm);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FarmDto>.Success(ToDto(farm));
    }

    public async Task<OperationResult<FarmDto>> UpdateAsync(Guid organizationId, Guid id, UpdateFarmCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.TotalAreaHectares <= 0)
            return OperationResult<FarmDto>.Validation("Name is required and totalAreaHectares must be greater than zero.");

        var farm = await repository.GetFarmAsync(organizationId, id, true, cancellationToken);
        if (farm is null)
            return OperationResult<FarmDto>.NotFound("Farm not found.");

        farm.Update(command.Name, command.TotalAreaHectares, command.City, command.State, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FarmDto>.Success(ToDto(farm));
    }

    public async Task<OperationResult<bool>> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var farm = await repository.GetFarmAsync(organizationId, id, true, cancellationToken);
        if (farm is null)
            return OperationResult<bool>.NotFound("Farm not found.");

        farm.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static FarmDto ToDto(Farm farm) => new(farm.Id, farm.Name, farm.TotalAreaHectares, farm.City, farm.State, farm.IsActive, farm.CreatedAtUtc, farm.UpdatedAtUtc);
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}
