using AgroControl.Application.Common;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Farms;

namespace AgroControl.Application.Production;

public sealed class FarmService(
    IProductionRepository repository,
    IMultiFarmRepository multiFarmRepository,
    IFarmAccessScope accessScope,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<FarmDto>> ListAsync(
        Guid organizationId,
        Guid userId,
        int page,
        int pageSize,
        string? search,
        bool includeInactive,
        Guid? regionId,
        string? stateCode,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var scope = await accessScope.GetEffectiveScopeAsync(organizationId, userId, cancellationToken);
        IReadOnlyCollection<Guid>? allowedFarmIds = scope.AllFarms ? null : scope.FarmIds;
        var (items, totalCount) = await repository.ListFarmsAsync(
            organizationId,
            (page - 1) * pageSize,
            pageSize,
            search,
            includeInactive,
            allowedFarmIds,
            regionId,
            stateCode,
            cancellationToken);
        return new PagedResult<FarmDto>(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<FarmDto?> GetAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        if (!await accessScope.CanAccessFarmAsync(organizationId, userId, id, cancellationToken)) return null;
        var farm = await repository.GetFarmAsync(organizationId, id, false, cancellationToken);
        return farm is null ? null : ToDto(farm);
    }

    public async Task<OperationResult<FarmDto>> CreateAsync(
        Guid organizationId,
        Guid userId,
        CreateFarmCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await accessScope.GetEffectiveScopeAsync(organizationId, userId, cancellationToken);
        if (!scope.AllFarms)
            return OperationResult<FarmDto>.Forbidden("Creating a farm requires explicit AllFarms access.");

        if (command.OperationalRegionId is not null)
        {
            var region = await multiFarmRepository.GetRegionAsync(organizationId, command.OperationalRegionId.Value, false, cancellationToken);
            if (region is null || !region.IsActive)
                return OperationResult<FarmDto>.Validation("Operational region does not exist or is inactive for this organization.");
        }

        try
        {
            var farm = Farm.Create(
                organizationId,
                command.Name,
                command.TotalAreaHectares,
                command.City,
                command.State,
                DateTime.UtcNow,
                command.OperationalRegionId,
                command.CountryCode,
                command.StateCode,
                command.MunicipalityCode,
                command.PostalCode,
                command.Latitude,
                command.Longitude,
                command.TimeZoneId);
            repository.AddFarm(farm);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<FarmDto>.Success(ToDto(farm));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<FarmDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<FarmDto>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        Guid id,
        UpdateFarmCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await accessScope.CanAccessFarmAsync(organizationId, userId, id, cancellationToken))
            return OperationResult<FarmDto>.NotFound("Farm not found.");

        var farm = await repository.GetFarmAsync(organizationId, id, true, cancellationToken);
        if (farm is null) return OperationResult<FarmDto>.NotFound("Farm not found.");

        if (farm.OperationalRegionId != command.OperationalRegionId)
        {
            var scope = await accessScope.GetEffectiveScopeAsync(organizationId, userId, cancellationToken);
            if (!scope.AllFarms)
                return OperationResult<FarmDto>.Forbidden("Changing a farm region requires explicit AllFarms access.");
        }

        if (command.OperationalRegionId is not null)
        {
            var region = await multiFarmRepository.GetRegionAsync(organizationId, command.OperationalRegionId.Value, false, cancellationToken);
            if (region is null || !region.IsActive)
                return OperationResult<FarmDto>.Validation("Operational region does not exist or is inactive for this organization.");
        }

        try
        {
            farm.Update(
                command.Name,
                command.TotalAreaHectares,
                command.City,
                command.State,
                DateTime.UtcNow,
                command.OperationalRegionId,
                command.CountryCode,
                command.StateCode,
                command.MunicipalityCode,
                command.PostalCode,
                command.Latitude,
                command.Longitude,
                command.TimeZoneId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<FarmDto>.Success(ToDto(farm));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<FarmDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> DeactivateAsync(
        Guid organizationId,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await accessScope.CanAccessFarmAsync(organizationId, userId, id, cancellationToken))
            return OperationResult<bool>.NotFound("Farm not found.");

        var farm = await repository.GetFarmAsync(organizationId, id, true, cancellationToken);
        if (farm is null) return OperationResult<bool>.NotFound("Farm not found.");
        farm.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static FarmDto ToDto(Farm farm) => new(
        farm.Id,
        farm.OperationalRegionId,
        farm.Name,
        farm.TotalAreaHectares,
        farm.City,
        farm.State,
        farm.CountryCode,
        farm.StateCode,
        farm.MunicipalityCode,
        farm.PostalCode,
        farm.Latitude,
        farm.Longitude,
        farm.TimeZoneId,
        farm.IsActive,
        farm.CreatedAtUtc,
        farm.UpdatedAtUtc);

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}
