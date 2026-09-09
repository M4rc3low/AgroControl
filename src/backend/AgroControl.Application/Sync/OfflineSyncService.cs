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
    IFarmAccessScope farmAccessScope,
    IOfflineSyncChangeRepository changeRepository,
    IOfflineSyncCursorProtector cursorProtector)
{
    private const int ProtocolVersion = 1;
    private const int LocalSchemaVersion = 1;
    private const int MaxBootstrapFields = 2_000;
    private const int MaxBootstrapSeasons = 10_000;
    private const int MaxBootstrapCrops = 1_000;
    private const int MaxPullChanges = 500;
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
        var cursorIssuedAtUtc = DateTime.UtcNow;
        var watermarkSequence = await changeRepository.GetCurrentSequenceAsync(
            organizationId,
            cancellationToken);
        var cursor = cursorProtector.Protect(
            organizationId,
            farmId,
            watermarkSequence,
            cursorIssuedAtUtc);

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
            DateTime.UtcNow,
            cursor,
            watermarkSequence));
    }

    public async Task<OperationResult<OfflinePullDto>> PullAsync(
        Guid organizationId,
        Guid userId,
        Guid farmId,
        string cursor,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var farmResult = await GetAuthorizedActiveFarmAsync(
            organizationId,
            userId,
            farmId,
            cancellationToken);

        if (!farmResult.Succeeded)
            return ConvertFailure<Farm, OfflinePullDto>(farmResult);

        var nowUtc = DateTime.UtcNow;
        var cursorValidation = cursorProtector.Validate(
            cursor,
            organizationId,
            farmId,
            nowUtc);
        if (!cursorValidation.IsValid)
            return OperationResult<OfflinePullDto>.Validation(cursorValidation.Error ?? "Sync cursor is invalid.");

        var pageSize = Math.Clamp(take, 1, MaxPullChanges);
        var entries = await changeRepository.ListAfterAsync(
            organizationId,
            farmId,
            cursorValidation.Sequence,
            pageSize + 1,
            cancellationToken);
        var hasMore = entries.Count > pageSize;
        var scanned = entries.Take(pageSize).ToArray();
        var changes = new List<OfflinePullChangeDto>(scanned.Length);

        foreach (var change in scanned)
        {
            var materialized = await MaterializeChangeAsync(
                organizationId,
                farmId,
                farmResult.Value!,
                change,
                cancellationToken);
            if (materialized is not null) changes.Add(materialized);
        }

        var lastSequence = scanned.Length == 0
            ? cursorValidation.Sequence
            : scanned[^1].Sequence;
        var nextCursor = cursorProtector.Protect(
            organizationId,
            farmId,
            lastSequence,
            nowUtc);

        return OperationResult<OfflinePullDto>.Success(new OfflinePullDto(
            farmId,
            changes,
            nextCursor,
            lastSequence,
            hasMore,
            nowUtc));
    }

    private async Task<OfflinePullChangeDto?> MaterializeChangeAsync(
        Guid organizationId,
        Guid farmId,
        Farm authorizedFarm,
        OfflineSyncChangeEntry change,
        CancellationToken cancellationToken)
    {
        if (change.OrganizationId != organizationId)
            return null;
        if (change.FarmId is not null && change.FarmId != farmId)
            return null;
        if (change.ChangeType == "delete")
            return ToDelete(change);
        if (change.ChangeType != "upsert")
            return null;

        object? payload;
        switch (change.EntityKind)
        {
            case "farm":
                if (change.EntityId != farmId) return null;
                payload = ToDto(authorizedFarm);
                break;

            case "field":
            {
                var field = await productionRepository.GetFieldAsync(
                    organizationId,
                    change.EntityId,
                    tracking: false,
                    cancellationToken);
                if (field is null || field.FarmId != farmId)
                    return ToDelete(change);
                payload = ToDto(field);
                break;
            }

            case "season":
            {
                var season = await productionRepository.GetSeasonAsync(
                    organizationId,
                    change.EntityId,
                    tracking: false,
                    cancellationToken);
                if (season is null)
                    return ToDelete(change);

                var field = await productionRepository.GetFieldAsync(
                    organizationId,
                    season.FieldId,
                    tracking: false,
                    cancellationToken);
                if (field is null || field.FarmId != farmId)
                    return ToDelete(change);

                payload = ToDto(season);
                break;
            }

            case "crop":
            {
                var crop = await productionRepository.GetCropAsync(
                    organizationId,
                    change.EntityId,
                    tracking: false,
                    cancellationToken);
                if (crop is null)
                    return ToDelete(change);
                payload = ToDto(crop);
                break;
            }

            default:
                return null;
        }

        return new OfflinePullChangeDto(
            change.Sequence,
            change.EntityKind,
            change.EntityId,
            "upsert",
            payload,
            change.OccurredAtUtc);
    }

    private static OfflinePullChangeDto ToDelete(OfflineSyncChangeEntry change) => new(
        change.Sequence,
        change.EntityKind,
        change.EntityId,
        "delete",
        null,
        change.OccurredAtUtc);

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
