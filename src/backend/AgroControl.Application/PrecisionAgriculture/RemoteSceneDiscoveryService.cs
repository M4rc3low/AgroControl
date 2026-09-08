using System.Text.Json;
using AgroControl.Application.Common;
using AgroControl.Application.Production;

namespace AgroControl.Application.PrecisionAgriculture;

public sealed class RemoteSceneDiscoveryService(
    IRemoteSceneDiscoveryClient client,
    IPrecisionAgricultureRepository precisionRepository,
    IProductionRepository productionRepository,
    RemoteSensingService remoteSensingService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IReadOnlyList<RemoteSceneDiscoveryProviderDto> GetProviders() =>
        client.GetProviders().Where(item => item.IsEnabled).ToArray();

    public async Task<RemoteSceneDiscoverySearchResult> SearchAsync(
        Guid organizationId,
        SearchRemoteScenesCommand command,
        CancellationToken cancellationToken = default)
    {
        var context = await ValidateContextAsync(organizationId, command.FieldId, command.SeasonId, true, cancellationToken);
        if (!context.Succeeded)
            return MapContextFailureToSearch(context);

        var request = new RemoteSceneDiscoverySearchRequest(
            command.Provider,
            command.FromUtc,
            command.ToUtc,
            context.Value!.BoundaryGeoJson!,
            command.PageSize,
            command.Collection,
            command.MaxCloudCoveragePercent,
            command.ContinuationToken);

        var result = await client.SearchAsync(request, cancellationToken);
        return result.ErrorKind switch
        {
            RemoteSceneDiscoveryCallErrorKind.None when result.Value is not null =>
                RemoteSceneDiscoverySearchResult.Success(result.Value),
            RemoteSceneDiscoveryCallErrorKind.Validation =>
                RemoteSceneDiscoverySearchResult.Validation(result.Error ?? "STAC search request is invalid."),
            RemoteSceneDiscoveryCallErrorKind.Timeout =>
                RemoteSceneDiscoverySearchResult.Timeout(result.Error ?? "STAC provider timed out."),
            RemoteSceneDiscoveryCallErrorKind.Unavailable =>
                RemoteSceneDiscoverySearchResult.Unavailable(result.Error ?? "STAC provider is unavailable."),
            RemoteSceneDiscoveryCallErrorKind.InvalidPayload =>
                RemoteSceneDiscoverySearchResult.InvalidPayload(result.Error ?? "STAC provider returned an invalid payload."),
            _ => RemoteSceneDiscoverySearchResult.Unavailable(result.Error ?? "STAC search failed.")
        };
    }

    public async Task<RemoteSceneDiscoveryImportResult> ImportAsync(
        Guid organizationId,
        ImportRemoteSceneCommand command,
        CancellationToken cancellationToken = default)
    {
        var context = await ValidateContextAsync(organizationId, command.FieldId, command.SeasonId, false, cancellationToken);
        if (!context.Succeeded)
            return MapContextFailureToImport(context);

        if (string.IsNullOrWhiteSpace(command.AssetKey))
            return RemoteSceneDiscoveryImportResult.Validation("Asset key is required.");

        var external = await client.GetItemAsync(command.Provider, command.Collection, command.ExternalId, cancellationToken);
        if (!external.Succeeded || external.Value is null)
            return external.ErrorKind switch
            {
                RemoteSceneDiscoveryCallErrorKind.Validation =>
                    RemoteSceneDiscoveryImportResult.Validation(external.Error ?? "STAC item request is invalid."),
                RemoteSceneDiscoveryCallErrorKind.NotFound =>
                    RemoteSceneDiscoveryImportResult.NotFound(external.Error ?? "STAC item was not found."),
                RemoteSceneDiscoveryCallErrorKind.Timeout =>
                    RemoteSceneDiscoveryImportResult.Timeout(external.Error ?? "STAC provider timed out."),
                RemoteSceneDiscoveryCallErrorKind.InvalidPayload =>
                    RemoteSceneDiscoveryImportResult.InvalidPayload(external.Error ?? "STAC item payload is invalid."),
                _ => RemoteSceneDiscoveryImportResult.Unavailable(external.Error ?? "STAC provider is unavailable.")
            };

        var item = external.Value;
        var asset = item.Assets.FirstOrDefault(candidate =>
            candidate.Key.Equals(command.AssetKey.Trim(), StringComparison.Ordinal) && candidate.IsRasterCandidate);
        if (asset is null)
            return RemoteSceneDiscoveryImportResult.Validation("Selected STAC asset is not an allowed raster candidate.");

        var footprint = TryParsePolygon(item.GeometryGeoJson);
        var notes = $"Imported from STAC collection {item.Collection}; asset {asset.Key}.";
        var create = new CreateRemoteSensingSceneCommand(
            command.FieldId,
            command.SeasonId,
            item.Provider,
            item.ExternalId,
            MapPlatform(item.Platform),
            item.AcquiredAtUtc,
            item.CloudCoveragePercent,
            item.SpatialResolutionMeters,
            asset.Href,
            notes,
            footprint);

        var created = await remoteSensingService.CreateSceneAsync(organizationId, create, cancellationToken);
        if (created.Succeeded && created.Value is not null)
            return RemoteSceneDiscoveryImportResult.Success(created.Value);

        return created.ErrorKind switch
        {
            OperationErrorKind.Validation =>
                RemoteSceneDiscoveryImportResult.Validation(created.Error ?? "Discovered scene could not be imported."),
            OperationErrorKind.NotFound or OperationErrorKind.Forbidden =>
                RemoteSceneDiscoveryImportResult.NotFound(created.Error ?? "Production context was not found."),
            OperationErrorKind.Conflict =>
                RemoteSceneDiscoveryImportResult.Conflict(created.Error ?? "The STAC scene was already imported."),
            _ => RemoteSceneDiscoveryImportResult.Unavailable(created.Error ?? "Scene import failed.")
        };
    }

    private async Task<OperationResult<SpatialFieldSnapshot>> ValidateContextAsync(
        Guid organizationId,
        Guid fieldId,
        Guid? seasonId,
        bool requireBoundary,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return OperationResult<SpatialFieldSnapshot>.Validation("Organization id is required.");
        if (fieldId == Guid.Empty)
            return OperationResult<SpatialFieldSnapshot>.Validation("Field id is required.");

        var field = await precisionRepository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        if (field is null)
            return OperationResult<SpatialFieldSnapshot>.NotFound("Field was not found in the current operational scope.");
        if (!field.IsActive)
            return OperationResult<SpatialFieldSnapshot>.Conflict("Inactive fields cannot be used for scene discovery or import.");
        if (requireBoundary && string.IsNullOrWhiteSpace(field.BoundaryGeoJson))
            return OperationResult<SpatialFieldSnapshot>.Validation("Field boundary is required for STAC scene discovery.");

        if (seasonId is { } seasonGuid)
        {
            var season = await productionRepository.GetSeasonAsync(organizationId, seasonGuid, false, cancellationToken);
            if (season is null)
                return OperationResult<SpatialFieldSnapshot>.NotFound("Season was not found in the current operational scope.");
            if (season.FieldId != fieldId)
                return OperationResult<SpatialFieldSnapshot>.Validation("Season must belong to the selected field.");
            if (!season.IsActive)
                return OperationResult<SpatialFieldSnapshot>.Conflict("Inactive seasons cannot receive discovered scenes.");
        }

        return OperationResult<SpatialFieldSnapshot>.Success(field);
    }

    private static RemoteSceneDiscoverySearchResult MapContextFailureToSearch(OperationResult<SpatialFieldSnapshot> result) =>
        result.ErrorKind switch
        {
            OperationErrorKind.Validation => RemoteSceneDiscoverySearchResult.Validation(result.Error ?? "Invalid discovery context."),
            OperationErrorKind.Conflict => RemoteSceneDiscoverySearchResult.Conflict(result.Error ?? "Discovery context is not active."),
            _ => RemoteSceneDiscoverySearchResult.NotFound(result.Error ?? "Discovery context was not found.")
        };

    private static RemoteSceneDiscoveryImportResult MapContextFailureToImport(OperationResult<SpatialFieldSnapshot> result) =>
        result.ErrorKind switch
        {
            OperationErrorKind.Validation => RemoteSceneDiscoveryImportResult.Validation(result.Error ?? "Invalid import context."),
            OperationErrorKind.Conflict => RemoteSceneDiscoveryImportResult.Conflict(result.Error ?? "Import context is not active."),
            _ => RemoteSceneDiscoveryImportResult.NotFound(result.Error ?? "Import context was not found.")
        };

    private static GeoJsonPolygonDto? TryParsePolygon(string? geometryGeoJson)
    {
        if (string.IsNullOrWhiteSpace(geometryGeoJson))
            return null;
        try
        {
            var polygon = JsonSerializer.Deserialize<GeoJsonPolygonDto>(geometryGeoJson, JsonOptions);
            return polygon is not null && polygon.Type.Equals("Polygon", StringComparison.OrdinalIgnoreCase)
                ? polygon
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string MapPlatform(string? platform) =>
        !string.IsNullOrWhiteSpace(platform) && platform.Contains("drone", StringComparison.OrdinalIgnoreCase)
            ? "Drone"
            : "Satellite";
}
