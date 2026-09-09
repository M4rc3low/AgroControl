using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.Application.Sync;

public sealed class OfflineSyncService(
    IProductionRepository productionRepository,
    IFarmAccessScope farmAccessScope)
{
    private const int ProtocolVersion = 1;
    private const int LocalSchemaVersion = 1;
    private const int MaxBootstrapFields = 2_000;
    private const int MaxBootstrapSeasons = 10_000;
    private const int MaxBootstrapCrops = 1_000;
    private static readonly string[] InitialEntityKinds = ["farm", "field", "crop", "season"];

    public async Task<OperationResult<OfflineSyncStatusDto>> GetStatusAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        var farmResult = await GetAuthorizedActiveFarmAsync(
            organizationId,
            userId,
            farmId,
            cancellationToken);

        if (!farmResult.Succeeded)
            return ConvertFailure<Farm, OfflineSyncStatusDto>(farmResult);

        var farm = farmResult.Value!;
        return OperationResult<OfflineSyncStatusDto>.Success(new OfflineSyncStatusDto(
            farm.Id,
            farm.Name,
            ProtocolVersion,
            LocalSchemaVersion,
            InitialEntityKinds,
            DateTime.UtcNow,
            farm.UpdatedAtUtc));
    }

    public async Task<OperationResult<OfflineBootstrapDto>> BootstrapAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        var farmResult = await GetAuthorizedActiveFarmAsync(
            organizationId,
            userId,
            farmId,
            cancellationToken);

        if (!farmResult.Succeeded)
            return ConvertFailure<Farm, OfflineBootstrapDto>(farmResult);

        var farm = farmResult.Value!;
        var allowedFarmIds = new[] { farmId };

        var (fields, totalFields) = await productionRepository.ListFieldsAsync(
            organizationId,
            0,
            MaxBootstrapFields + 1,
            farmId,
            search: null,
            includeInactive: true,
            allowedFarmIds,
            cancellationToken);

        if (totalFields > MaxBootstrapFields)
        {
            return OperationResult<OfflineBootstrapDto>.Validation(
                $"Farm exceeds the offline bootstrap limit of {MaxBootstrapFields} fields.");
        }

        var (seasons, totalSeasons) = await productionRepository.ListSeasonsAsync(
            organizationId,
            0,
            MaxBootstrapSeasons + 1,
            fieldId: null,
            status: null,
            search: null,
            includeInactive: true,
            allowedFarmIds,
            cancellationToken);

        if (totalSeasons > MaxBootstrapSeasons)
        {
            return OperationResult<OfflineBootstrapDto>.Validation(
                $"Farm exceeds the offline bootstrap limit of {MaxBootstrapSeasons} seasons.");
        }

        var cropIds = seasons.Select(item => item.CropId).Distinct().ToArray();
        if (cropIds.Length > MaxBootstrapCrops)
        {
            return OperationResult<OfflineBootstrapDto>.Validation(
                $"Farm exceeds the offline bootstrap limit of {MaxBootstrapCrops} referenced crops.");
        }

        var crops = new List<Crop>(cropIds.Length);
        foreach (var cropId in cropIds)
        {
            var crop = await productionRepository.GetCropAsync(
                organizationId,
                cropId,
                tracking: false,
                cancellationToken);

            if (crop is null)
            {
                return OperationResult<OfflineBootstrapDto>.Conflict(
                    "Offline bootstrap found a season whose crop no longer exists.");
            }

            crops.Add(crop);
        }

        return OperationResult<OfflineBootstrapDto>.Success(new OfflineBootstrapDto(
            ToDto(farm),
            fields.Select(ToDto).ToArray(),
            crops.Select(ToDto).ToArray(),
            seasons.Select(ToDto).ToArray(),
            ProtocolVersion,
            LocalSchemaVersion,
            DateTime.UtcNow));
    }

    private async Task<OperationResult<Farm>> GetAuthorizedActiveFarmAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken)
    {
        if (!await farmAccessScope.CanAccessFarmAsync(organizationId, userId, farmId, cancellationToken))
        {
            return OperationResult<Farm>.Forbidden(
                "Farm access is not available for offline synchronization.");
        }

        var farm = await productionRepository.GetFarmAsync(
            organizationId,
            farmId,
            tracking: false,
            cancellationToken);

        return farm is null || !farm.IsActive
            ? OperationResult<Farm>.NotFound("Farm not found or inactive.")
            : OperationResult<Farm>.Success(farm);
    }

    private static OperationResult<TTarget> ConvertFailure<TSource, TTarget>(OperationResult<TSource> source) =>
        source.ErrorKind switch
        {
            OperationErrorKind.Validation => OperationResult<TTarget>.Validation(source.Error ?? "Invalid sync request."),
            OperationErrorKind.NotFound => OperationResult<TTarget>.NotFound(source.Error ?? "Resource not found."),
            OperationErrorKind.Conflict => OperationResult<TTarget>.Conflict(source.Error ?? "Sync conflict."),
            OperationErrorKind.Forbidden => OperationResult<TTarget>.Forbidden(source.Error ?? "Offline synchronization access denied."),
            _ => throw new InvalidOperationException("A failed sync operation must contain an error kind.")
        };

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

    private static FieldDto ToDto(Field field) => new(
        field.Id,
        field.FarmId,
        field.Name,
        field.AreaHectares,
        field.IsActive,
        field.CreatedAtUtc,
        field.UpdatedAtUtc);

    private static CropDto ToDto(Crop crop) => new(
        crop.Id,
        crop.Name,
        crop.Variety,
        crop.IsActive,
        crop.CreatedAtUtc,
        crop.UpdatedAtUtc);

    private static SeasonDto ToDto(Season season) => new(
        season.Id,
        season.FieldId,
        season.CropId,
        season.Name,
        season.StartDate,
        season.EndDate,
        season.ExpectedYieldPerHectare,
        season.ActualYieldPerHectare,
        season.Status.ToString(),
        season.IsActive,
        season.CreatedAtUtc,
        season.UpdatedAtUtc);
}
