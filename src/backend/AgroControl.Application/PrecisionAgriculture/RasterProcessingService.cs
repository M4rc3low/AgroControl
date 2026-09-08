using System.Security.Cryptography;
using System.Text;
using AgroControl.Application.Common;
using AgroControl.Application.Intelligence;
using AgroControl.Domain.Modules.PrecisionAgriculture;

namespace AgroControl.Application.PrecisionAgriculture;

public sealed class RasterProcessingService(
    IRemoteSensingRepository remoteRepository,
    IPrecisionAgricultureRepository precisionRepository,
    IRasterProcessingRepository rasterRepository,
    IIntelligenceClient intelligenceClient)
{
    public async Task<RasterProcessResult> ProcessSceneAsync(Guid organizationId, Guid sceneId,
        ProcessRasterSceneCommand command, CancellationToken cancellationToken = default)
    {
        var scene = await remoteRepository.GetSceneAsync(organizationId, sceneId, cancellationToken);
        if (scene is null) return RasterProcessResult.NotFound("Remote-sensing scene was not found in the current organization.");
        if (!scene.IsActive) return RasterProcessResult.Conflict("Inactive scenes cannot be processed.");
        if (!TryParseProductType(command.ProductType, out var productType))
            return RasterProcessResult.Validation("Product type must be NDVI, NDRE, EVI or Custom.");

        var customName = Clean(command.CustomProductName);
        if (productType == RasterProductType.Custom && string.IsNullOrWhiteSpace(customName))
            return RasterProcessResult.Validation("Custom product name is required for Custom raster products.");
        if (customName?.Length > 120) return RasterProcessResult.Validation("Custom product name cannot exceed 120 characters.");
        if (command.Band is < 1 or > 128) return RasterProcessResult.Validation("Raster band must be between 1 and 128.");

        var assetReference = Clean(scene.AssetReference);
        if (string.IsNullOrWhiteSpace(assetReference))
            return RasterProcessResult.Validation("The scene must reference a raster asset before processing.");
        if (assetReference.Length > 2048) return RasterProcessResult.Validation("Raster asset reference cannot exceed 2048 characters.");

        var requestedAsset = Clean(command.AssetReference);
        if (requestedAsset is not null && !string.Equals(requestedAsset, assetReference, StringComparison.Ordinal))
            return RasterProcessResult.Validation("Raster processing is limited to the asset explicitly referenced by the selected scene.");

        var field = await precisionRepository.GetFieldAsync(organizationId, scene.FieldId, cancellationToken);
        if (field is null || !field.IsActive) return RasterProcessResult.NotFound("Scene field was not found or is inactive.");
        if (string.IsNullOrWhiteSpace(field.BoundaryGeoJson))
            return RasterProcessResult.Validation("The field must have a georeferenced boundary before raster processing.");

        var zones = command.IncludeManagementZones
            ? await precisionRepository.ListZonesAsync(organizationId, scene.FieldId, null, null, false, cancellationToken)
            : [];
        var targets = new List<RasterTargetData> { new("field", field.BoundaryGeoJson) };
        var targetZones = new Dictionary<string, Guid?> { ["field"] = null };
        foreach (var zone in zones.Where(item => item.IsActive))
        {
            var key = $"zone:{zone.Id:D}";
            targets.Add(new RasterTargetData(key, zone.GeometryGeoJson));
            targetZones[key] = zone.Id;
        }

        var clientProcessingKey = NormalizeProcessingKey(command.ProcessingKey);
        if (clientProcessingKey?.Length > 160)
            return RasterProcessResult.Validation("Processing key cannot exceed 160 characters.");
        var processingKey = ComputeProcessingKey(scene.Id, productType, customName, assetReference, command.Band,
            command.IncludeManagementZones, clientProcessingKey, targets);

        var now = DateTime.UtcNow;
        var product = new RasterProductWriteModel(Guid.NewGuid(), scene.Id, productType.ToString(), customName,
            assetReference, command.Band, now);
        var run = new RasterProcessingRunWriteModel(Guid.NewGuid(), product.Id, scene.Id, processingKey,
            command.IncludeManagementZones, now);
        var creation = await rasterRepository.CreateRunAsync(organizationId, product, run, cancellationToken);
        if (!creation.Created)
        {
            var existingResults = await rasterRepository.ListResultsByRunAsync(organizationId, creation.Run.Id, cancellationToken);
            return RasterProcessResult.Success(new RasterProcessingResponseDto(ToRunDto(creation.Run), true,
                existingResults.Select(ToResultDto).ToList()));
        }

        var processing = await rasterRepository.MarkProcessingAsync(organizationId, creation.Run.Id, DateTime.UtcNow, cancellationToken);
        if (processing is null) return RasterProcessResult.Conflict("Raster processing could not transition to Processing.");

        var call = await intelligenceClient.ProcessRasterAsync(new RasterProcessingData(assetReference, "EPSG:4326",
            command.Band, targets), cancellationToken);
        if (!call.Succeeded || call.Value is null)
        {
            var failure = SanitizeFailure(call.Error ?? "Raster processing failed.");
            await rasterRepository.MarkFailedAsync(organizationId, processing.Id, failure, DateTime.UtcNow, cancellationToken);
            return call.ErrorKind switch
            {
                IntelligenceCallErrorKind.Validation => RasterProcessResult.Validation(failure),
                IntelligenceCallErrorKind.Timeout => RasterProcessResult.Timeout(failure),
                _ => RasterProcessResult.Unavailable(failure)
            };
        }

        var returnedKeys = call.Value.Results.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        if (returnedKeys.Count != targets.Count || targets.Any(target => !returnedKeys.Contains(target.Key)))
        {
            const string failure = "Intelligence service returned an incomplete or unexpected raster target set.";
            await rasterRepository.MarkFailedAsync(organizationId, processing.Id, failure, DateTime.UtcNow, cancellationToken);
            return RasterProcessResult.Unavailable(failure);
        }

        var writes = new List<RasterZonalResultWriteModel>(call.Value.Results.Count);
        try
        {
            foreach (var item in call.Value.Results)
            {
                var zoneId = targetZones[item.Key];
                var observation = VegetationIndexObservation.Create(organizationId, scene.Id, scene.FieldId, scene.SeasonId,
                    zoneId, ToVegetationIndex(productType), customName, item.Minimum, item.Maximum, item.Mean, item.Median,
                    item.StandardDeviation, item.ValidCoveragePercent, item.SampleCount, scene.Provider, scene.AcquiredAtUtc,
                    DateTime.UtcNow);
                writes.Add(new RasterZonalResultWriteModel(Guid.NewGuid(), observation.Id, product.Id, scene.Id,
                    scene.FieldId, scene.SeasonId, zoneId, observation.IndexType.ToString(), observation.CustomIndexName,
                    observation.Minimum, observation.Maximum, observation.Mean, observation.Median,
                    observation.StandardDeviation, observation.ValidCoveragePercent, observation.SampleCount ?? 0,
                    observation.Source, observation.ObservedAtUtc, observation.CreatedAtUtc));
            }
        }
        catch (ArgumentException ex)
        {
            var failure = SanitizeFailure(ex.Message);
            await rasterRepository.MarkFailedAsync(organizationId, processing.Id, failure, DateTime.UtcNow, cancellationToken);
            return RasterProcessResult.Validation(failure);
        }

        var metadata = new RasterCompletionMetadata(call.Value.Metadata.Crs, call.Value.Metadata.ResolutionX,
            call.Value.Metadata.ResolutionY, call.Value.Metadata.Nodata, call.Value.Metadata.Width, call.Value.Metadata.Height);
        var completed = await rasterRepository.CompleteAsync(organizationId, processing.Id, metadata, writes, cancellationToken);
        if (completed is null)
        {
            await rasterRepository.MarkFailedAsync(organizationId, processing.Id,
                "Raster results could not be persisted atomically.", DateTime.UtcNow, cancellationToken);
            return RasterProcessResult.Conflict("Raster results could not be persisted atomically.");
        }

        var results = await rasterRepository.ListResultsByRunAsync(organizationId, completed.Id, cancellationToken);
        return RasterProcessResult.Success(new RasterProcessingResponseDto(ToRunDto(completed), false,
            results.Select(ToResultDto).ToList()));
    }

    public async Task<IReadOnlyList<RasterProcessingRunDto>> ListRunsAsync(Guid organizationId, Guid sceneId,
        CancellationToken cancellationToken = default) =>
        (await rasterRepository.ListRunsAsync(organizationId, sceneId, cancellationToken)).Select(ToRunDto).ToList();

    public async Task<PagedResult<RasterZonalResultDto>> ListResultsAsync(Guid organizationId, int page, int pageSize,
        Guid? fieldId, Guid? seasonId, Guid? managementZoneId, string? indexType, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedType = NormalizeIndexType(indexType);
        var (items, total) = await rasterRepository.ListResultsAsync(organizationId, (page - 1) * pageSize, pageSize,
            fieldId, seasonId, managementZoneId, normalizedType, NormalizeUtc(fromUtc), NormalizeUtc(toUtc), cancellationToken);
        return new PagedResult<RasterZonalResultDto>(items.Select(ToResultDto).ToList(), page, pageSize, total);
    }

    private static bool TryParseProductType(string? value, out RasterProductType type) =>
        Enum.TryParse(value, true, out type) && Enum.IsDefined(type);

    private static VegetationIndexType ToVegetationIndex(RasterProductType type) => type switch
    {
        RasterProductType.NDVI => VegetationIndexType.NDVI,
        RasterProductType.NDRE => VegetationIndexType.NDRE,
        RasterProductType.EVI => VegetationIndexType.EVI,
        _ => VegetationIndexType.Custom
    };

    private static string? NormalizeIndexType(string? value) =>
        TryParseProductType(value, out var type) ? type.ToString() : null;

    private static string ComputeProcessingKey(Guid sceneId, RasterProductType type, string? customName,
        string assetReference, int band, bool includeZones, string? clientKey, IReadOnlyList<RasterTargetData> targets)
    {
        var spatialContext = string.Join('|', targets
            .OrderBy(target => target.Key, StringComparer.Ordinal)
            .Select(target => $"{target.Key}:{target.GeometryGeoJson}"));
        var material = $"v2|{sceneId:D}|{type}|{customName}|{assetReference}|{band}|{includeZones}|{clientKey}|{spatialContext}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }

    private static string? NormalizeProcessingKey(string? value) => Clean(value);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string SanitizeFailure(string value) => value.Trim().Length <= 500 ? value.Trim() : value.Trim()[..500];
    private static DateTime? NormalizeUtc(DateTime? value) => value is null ? null : value.Value.Kind switch
    {
        DateTimeKind.Utc => value.Value,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };

    private static RasterProcessingRunDto ToRunDto(RasterProcessingRunSnapshot item) => new(item.Id, item.ProductId,
        item.SceneId, item.ProcessingKey, item.Status, item.IncludeManagementZones, item.RequestedAtUtc, item.StartedAtUtc,
        item.CompletedAtUtc, item.FailureMessage, item.ProductType, item.CustomProductName, item.AssetReference, item.Band,
        item.Crs, item.ResolutionX, item.ResolutionY, item.Nodata, item.Width, item.Height);

    private static RasterZonalResultDto ToResultDto(RasterZonalResultSnapshot item) => new(item.Id, item.RunId,
        item.ProductId, item.ObservationId, item.SceneId, item.FieldId, item.SeasonId, item.ManagementZoneId, item.IndexType,
        item.CustomIndexName, item.Minimum, item.Maximum, item.Mean, item.Median, item.StandardDeviation,
        item.ValidCoveragePercent, item.SampleCount, item.Source, item.ObservedAtUtc, item.CreatedAtUtc);
}
