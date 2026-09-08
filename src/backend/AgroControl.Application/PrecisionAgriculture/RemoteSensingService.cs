using System.Text.Json;
using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.PrecisionAgriculture;

namespace AgroControl.Application.PrecisionAgriculture;

public sealed class RemoteSensingService(
    IRemoteSensingRepository repository,
    IProductionRepository productionRepository,
    IPrecisionAgricultureRepository precisionRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResult<RemoteSensingSceneDto>> ListScenesAsync(Guid organizationId, int page, int pageSize,
        Guid? fieldId, Guid? seasonId, string? platform, string? provider, DateTime? fromUtc, DateTime? toUtc,
        bool includeInactive, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedPlatform = NormalizePlatformFilter(platform);
        var (items, total) = await repository.ListScenesAsync(organizationId, (page - 1) * pageSize, pageSize,
            fieldId, seasonId, normalizedPlatform, Clean(provider), NormalizeUtc(fromUtc), NormalizeUtc(toUtc), includeInactive, cancellationToken);
        return new PagedResult<RemoteSensingSceneDto>(items.Select(ToSceneDto).ToList(), page, pageSize, total);
    }

    public async Task<RemoteSensingSceneDto?> GetSceneAsync(Guid organizationId, Guid sceneId, CancellationToken cancellationToken = default)
    {
        var scene = await repository.GetSceneAsync(organizationId, sceneId, cancellationToken);
        return scene is null ? null : ToSceneDto(scene);
    }

    public async Task<OperationResult<RemoteSensingSceneDto>> CreateSceneAsync(Guid organizationId,
        CreateRemoteSensingSceneCommand command, CancellationToken cancellationToken = default)
    {
        var context = await ValidateProductionContextAsync(organizationId, command.FieldId, command.SeasonId, cancellationToken);
        if (!context.Succeeded) return CopyFailure<bool, RemoteSensingSceneDto>(context);

        if (!TryParsePlatform(command.Platform, out var platform))
            return OperationResult<RemoteSensingSceneDto>.Validation("Platform must be Satellite, Drone or Other.");

        var footprint = NormalizeFootprint(command.Footprint);
        if (!footprint.Succeeded) return CopyFailure<string?, RemoteSensingSceneDto>(footprint);

        RemoteSensingScene scene;
        try
        {
            scene = RemoteSensingScene.Create(organizationId, command.FieldId, command.SeasonId, command.Provider,
                command.ExternalId, platform, command.AcquiredAtUtc, command.CloudCoveragePercent,
                command.SpatialResolutionMeters, command.AssetReference, command.Notes, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<RemoteSensingSceneDto>.Validation(ex.Message);
        }

        var write = new RemoteSensingSceneWriteModel(scene.Id, scene.FieldId, scene.SeasonId, scene.Provider, scene.ExternalId,
            scene.Platform.ToString(), scene.AcquiredAtUtc, scene.CloudCoveragePercent, scene.SpatialResolutionMeters,
            scene.AssetReference, scene.Notes, footprint.Value, scene.CreatedAtUtc, scene.UpdatedAtUtc);
        var created = await repository.AddSceneAsync(organizationId, write, cancellationToken);
        return created is null
            ? OperationResult<RemoteSensingSceneDto>.Conflict("A scene with the same provider and external id already exists in this organization, or its footprint is invalid.")
            : OperationResult<RemoteSensingSceneDto>.Success(ToSceneDto(created));
    }

    public async Task<OperationResult<RemoteSensingSceneDto>> UpdateSceneAsync(Guid organizationId, Guid sceneId,
        UpdateRemoteSensingSceneCommand command, CancellationToken cancellationToken = default)
    {
        var current = await repository.GetSceneAsync(organizationId, sceneId, cancellationToken);
        if (current is null) return OperationResult<RemoteSensingSceneDto>.NotFound("Remote-sensing scene was not found in the current organization.");
        if (!current.IsActive) return OperationResult<RemoteSensingSceneDto>.Conflict("Inactive scenes cannot be edited.");

        var context = await ValidateProductionContextAsync(organizationId, command.FieldId, command.SeasonId, cancellationToken);
        if (!context.Succeeded) return CopyFailure<bool, RemoteSensingSceneDto>(context);
        if (!TryParsePlatform(command.Platform, out var platform))
            return OperationResult<RemoteSensingSceneDto>.Validation("Platform must be Satellite, Drone or Other.");

        var footprint = NormalizeFootprint(command.Footprint);
        if (!footprint.Succeeded) return CopyFailure<string?, RemoteSensingSceneDto>(footprint);

        RemoteSensingScene candidate;
        try
        {
            candidate = RemoteSensingScene.Create(organizationId, command.FieldId, command.SeasonId, current.Provider,
                current.ExternalId, platform, command.AcquiredAtUtc, command.CloudCoveragePercent,
                command.SpatialResolutionMeters, command.AssetReference, command.Notes, current.CreatedAtUtc);
            candidate.UpdateContext(command.FieldId, command.SeasonId, platform, command.AcquiredAtUtc,
                command.CloudCoveragePercent, command.SpatialResolutionMeters, command.AssetReference, command.Notes, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<RemoteSensingSceneDto>.Validation(ex.Message);
        }

        var write = new RemoteSensingSceneWriteModel(current.Id, candidate.FieldId, candidate.SeasonId, current.Provider,
            current.ExternalId, candidate.Platform.ToString(), candidate.AcquiredAtUtc, candidate.CloudCoveragePercent,
            candidate.SpatialResolutionMeters, candidate.AssetReference, candidate.Notes, footprint.Value,
            current.CreatedAtUtc, candidate.UpdatedAtUtc);
        var updated = await repository.UpdateSceneAsync(organizationId, write, cancellationToken);
        return updated is null
            ? OperationResult<RemoteSensingSceneDto>.Validation("The scene footprint is topologically invalid, or the production context is no longer valid.")
            : OperationResult<RemoteSensingSceneDto>.Success(ToSceneDto(updated));
    }

    public async Task<OperationResult<RemoteSensingSceneDto>> DeactivateSceneAsync(Guid organizationId, Guid sceneId,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.GetSceneAsync(organizationId, sceneId, cancellationToken);
        if (current is null) return OperationResult<RemoteSensingSceneDto>.NotFound("Remote-sensing scene was not found in the current organization.");
        var updated = await repository.DeactivateSceneAsync(organizationId, sceneId, DateTime.UtcNow, cancellationToken);
        return updated is null
            ? OperationResult<RemoteSensingSceneDto>.NotFound("Remote-sensing scene was not found in the current organization.")
            : OperationResult<RemoteSensingSceneDto>.Success(ToSceneDto(updated));
    }

    public async Task<PagedResult<VegetationIndexObservationDto>> ListObservationsAsync(Guid organizationId,
        int page, int pageSize, Guid? sceneId, Guid? fieldId, Guid? seasonId, Guid? managementZoneId,
        string? indexType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedType = NormalizeIndexTypeFilter(indexType);
        var (items, total) = await repository.ListObservationsAsync(organizationId, (page - 1) * pageSize, pageSize,
            sceneId, fieldId, seasonId, managementZoneId, normalizedType, NormalizeUtc(fromUtc), NormalizeUtc(toUtc), cancellationToken);
        return new PagedResult<VegetationIndexObservationDto>(items.Select(ToObservationDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<VegetationIndexObservationDto>> CreateObservationAsync(Guid organizationId,
        CreateVegetationIndexObservationCommand command, CancellationToken cancellationToken = default)
    {
        var scene = await repository.GetSceneAsync(organizationId, command.SceneId, cancellationToken);
        if (scene is null) return OperationResult<VegetationIndexObservationDto>.NotFound("Remote-sensing scene was not found in the current organization.");
        if (!scene.IsActive) return OperationResult<VegetationIndexObservationDto>.Conflict("New observations cannot be added to an inactive scene.");

        if (!TryParseIndexType(command.IndexType, out var indexType))
            return OperationResult<VegetationIndexObservationDto>.Validation("Index type must be NDVI, NDRE, EVI or Custom.");

        if (command.ManagementZoneId is { } zoneId)
        {
            var zone = await precisionRepository.GetZoneAsync(organizationId, zoneId, cancellationToken);
            if (zone is null || !zone.IsActive)
                return OperationResult<VegetationIndexObservationDto>.NotFound("Management zone was not found or is inactive in the current organization.");
            if (zone.FieldId != scene.FieldId)
                return OperationResult<VegetationIndexObservationDto>.Validation("Management zone must belong to the same field as the scene.");
        }

        VegetationIndexObservation observation;
        try
        {
            observation = VegetationIndexObservation.Create(organizationId, scene.Id, scene.FieldId, scene.SeasonId,
                command.ManagementZoneId, indexType, command.CustomIndexName, command.Minimum, command.Maximum,
                command.Mean, command.Median, command.StandardDeviation, command.ValidCoveragePercent,
                command.SampleCount, scene.Provider, scene.AcquiredAtUtc, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<VegetationIndexObservationDto>.Validation(ex.Message);
        }

        var write = new VegetationIndexObservationWriteModel(observation.Id, observation.SceneId, observation.FieldId,
            observation.SeasonId, observation.ManagementZoneId, observation.IndexType.ToString(), observation.CustomIndexName,
            observation.Minimum, observation.Maximum, observation.Mean, observation.Median, observation.StandardDeviation,
            observation.ValidCoveragePercent, observation.SampleCount, observation.Source, observation.ObservedAtUtc,
            observation.CreatedAtUtc);
        var created = await repository.AddObservationAsync(organizationId, write, cancellationToken);
        return created is null
            ? OperationResult<VegetationIndexObservationDto>.Validation("The observation context is no longer valid for this organization.")
            : OperationResult<VegetationIndexObservationDto>.Success(ToObservationDto(created));
    }

    public async Task<OperationResult<IReadOnlyList<VegetationIndexSeriesPointDto>>> GetSeriesAsync(Guid organizationId,
        Guid fieldId, Guid? seasonId, Guid? managementZoneId, string? indexType, DateTime? fromUtc, DateTime? toUtc,
        int take = 500, CancellationToken cancellationToken = default)
    {
        var context = await ValidateProductionContextAsync(organizationId, fieldId, seasonId, cancellationToken);
        if (!context.Succeeded) return CopyFailure<bool, IReadOnlyList<VegetationIndexSeriesPointDto>>(context);
        if (managementZoneId is { } zoneId)
        {
            var zone = await precisionRepository.GetZoneAsync(organizationId, zoneId, cancellationToken);
            if (zone is null || zone.FieldId != fieldId)
                return OperationResult<IReadOnlyList<VegetationIndexSeriesPointDto>>.NotFound("Management zone was not found in the selected field.");
        }
        var normalizedType = NormalizeIndexTypeFilter(indexType);
        if (!string.IsNullOrWhiteSpace(indexType) && normalizedType is null)
            return OperationResult<IReadOnlyList<VegetationIndexSeriesPointDto>>.Validation("Index type must be NDVI, NDRE, EVI or Custom.");
        var items = await repository.ListSeriesAsync(organizationId, fieldId, seasonId, managementZoneId, normalizedType,
            NormalizeUtc(fromUtc), NormalizeUtc(toUtc), Math.Clamp(take, 1, 1000), cancellationToken);
        return OperationResult<IReadOnlyList<VegetationIndexSeriesPointDto>>.Success(items.Select(ToSeriesPoint).ToList());
    }

    public async Task<OperationResult<RemoteSensingSummaryDto>> GetSummaryAsync(Guid organizationId, Guid fieldId,
        Guid? seasonId, Guid? managementZoneId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var context = await ValidateProductionContextAsync(organizationId, fieldId, seasonId, cancellationToken);
        if (!context.Succeeded) return CopyFailure<bool, RemoteSensingSummaryDto>(context);
        if (managementZoneId is { } zoneId)
        {
            var zone = await precisionRepository.GetZoneAsync(organizationId, zoneId, cancellationToken);
            if (zone is null || zone.FieldId != fieldId)
                return OperationResult<RemoteSensingSummaryDto>.NotFound("Management zone was not found in the selected field.");
        }

        var from = NormalizeUtc(fromUtc); var to = NormalizeUtc(toUtc);
        var series = await repository.ListSeriesAsync(organizationId, fieldId, seasonId, managementZoneId, null, from, to, 1000, cancellationToken);
        var latest = series
            .GroupBy(item => new { item.IndexType, item.CustomIndexName })
            .Select(group => group.OrderByDescending(item => item.ObservedAtUtc).ThenByDescending(item => item.CreatedAtUtc).First())
            .OrderBy(item => item.IndexType)
            .Select(item => new RemoteSensingLatestMetricDto(item.IndexType, item.CustomIndexName, item.Mean, item.Minimum,
                item.Maximum, item.ValidCoveragePercent, item.Source, item.Platform, item.ObservedAtUtc, item.SceneId,
                item.ManagementZoneId))
            .ToList();
        var sceneCount = await repository.CountScenesAsync(organizationId, fieldId, seasonId, from, to, cancellationToken);
        return OperationResult<RemoteSensingSummaryDto>.Success(new RemoteSensingSummaryDto(fieldId, seasonId,
            managementZoneId, sceneCount, latest));
    }

    private async Task<OperationResult<bool>> ValidateProductionContextAsync(Guid organizationId, Guid fieldId,
        Guid? seasonId, CancellationToken cancellationToken)
    {
        var field = await productionRepository.GetFieldAsync(organizationId, fieldId, false, cancellationToken);
        if (field is null) return OperationResult<bool>.NotFound("Field was not found in the current organization.");
        if (!field.IsActive) return OperationResult<bool>.Conflict("Inactive fields cannot receive remote-sensing data.");
        if (seasonId is not { } seasonGuid) return OperationResult<bool>.Success(true);
        var season = await productionRepository.GetSeasonAsync(organizationId, seasonGuid, false, cancellationToken);
        if (season is null) return OperationResult<bool>.NotFound("Season was not found in the current organization.");
        if (season.FieldId != fieldId) return OperationResult<bool>.Validation("Season must belong to the selected field.");
        if (!season.IsActive) return OperationResult<bool>.Conflict("Inactive seasons cannot receive new remote-sensing scenes.");
        return OperationResult<bool>.Success(true);
    }

    private static OperationResult<string?> NormalizeFootprint(GeoJsonPolygonDto? footprint)
    {
        if (footprint is null) return OperationResult<string?>.Success(null);
        var validation = GeoJsonPolygonValidator.ValidateAndNormalize(footprint);
        return validation.Succeeded
            ? OperationResult<string?>.Success(JsonSerializer.Serialize(validation.Value, JsonOptions))
            : OperationResult<string?>.Validation(validation.Error ?? "Invalid footprint GeoJSON polygon.");
    }

    private static RemoteSensingSceneDto ToSceneDto(RemoteSensingSceneSnapshot item) => new(item.Id, item.FieldId,
        item.SeasonId, item.Provider, item.ExternalId, item.Platform, item.AcquiredAtUtc, item.CloudCoveragePercent,
        item.SpatialResolutionMeters, item.AssetReference, item.Notes,
        string.IsNullOrWhiteSpace(item.FootprintGeoJson) ? null : JsonSerializer.Deserialize<GeoJsonPolygonDto>(item.FootprintGeoJson, JsonOptions),
        item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static VegetationIndexObservationDto ToObservationDto(VegetationIndexObservationSnapshot item) => new(
        item.Id, item.SceneId, item.FieldId, item.SeasonId, item.ManagementZoneId, item.IndexType, item.CustomIndexName,
        item.Minimum, item.Maximum, item.Mean, item.Median, item.StandardDeviation, item.ValidCoveragePercent,
        item.SampleCount, item.Source, item.Platform, item.ObservedAtUtc, item.CreatedAtUtc);

    private static VegetationIndexSeriesPointDto ToSeriesPoint(VegetationIndexObservationSnapshot item) => new(
        item.Id, item.SceneId, item.ManagementZoneId, item.IndexType, item.CustomIndexName, item.Mean, item.Minimum,
        item.Maximum, item.Median, item.StandardDeviation, item.ValidCoveragePercent, item.SampleCount, item.Source,
        item.Platform, item.ObservedAtUtc);

    private static string? NormalizePlatformFilter(string? value) =>
        TryParsePlatform(value, out var platform) ? platform.ToString() : null;

    private static string? NormalizeIndexTypeFilter(string? value) =>
        TryParseIndexType(value, out var indexType) ? indexType.ToString() : null;

    private static bool TryParsePlatform(string? value, out RemoteSensingPlatform platform) =>
        Enum.TryParse(value, true, out platform) && Enum.IsDefined(platform);

    private static bool TryParseIndexType(string? value, out VegetationIndexType indexType) =>
        Enum.TryParse(value, true, out indexType) && Enum.IsDefined(indexType);

    private static DateTime? NormalizeUtc(DateTime? value) => value is null ? null : value.Value.Kind switch
    {
        DateTimeKind.Utc => value.Value,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OperationResult<TTo> CopyFailure<TFrom, TTo>(OperationResult<TFrom> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => OperationResult<TTo>.Validation(result.Error ?? "Validation failed."),
        OperationErrorKind.NotFound => OperationResult<TTo>.NotFound(result.Error ?? "Resource was not found."),
        OperationErrorKind.Conflict => OperationResult<TTo>.Conflict(result.Error ?? "Operation conflicts with current state."),
        _ => OperationResult<TTo>.Validation(result.Error ?? "Operation failed.")
    };
}
