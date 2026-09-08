namespace AgroControl.Application.PrecisionAgriculture;

public sealed record RemoteSceneDiscoveryProviderDto(
    string Key,
    string DisplayName,
    bool IsEnabled,
    IReadOnlyList<string> Collections);

public sealed record RemoteSceneDiscoverySearchRequest(
    string Provider,
    DateTime FromUtc,
    DateTime ToUtc,
    string GeometryGeoJson,
    int PageSize = 30,
    string? Collection = null,
    decimal? MaxCloudCoveragePercent = null,
    string? ContinuationToken = null);

public sealed record RemoteSceneDiscoveryAssetDto(
    string Key,
    string Href,
    string? MediaType,
    IReadOnlyList<string> Roles,
    bool IsRasterCandidate);

public sealed record RemoteSceneDiscoveryItemDto(
    string Provider,
    string Collection,
    string ExternalId,
    DateTime AcquiredAtUtc,
    string? GeometryGeoJson,
    IReadOnlyList<double>? Bbox,
    decimal? CloudCoveragePercent,
    decimal? SpatialResolutionMeters,
    string? Platform,
    string? Constellation,
    IReadOnlyList<RemoteSceneDiscoveryAssetDto> Assets);

public sealed record RemoteSceneDiscoveryPageDto(
    IReadOnlyList<RemoteSceneDiscoveryItemDto> Items,
    string? ContinuationToken);

public enum RemoteSceneDiscoveryCallErrorKind
{
    None = 0,
    Validation = 1,
    Timeout = 2,
    Unavailable = 3,
    InvalidPayload = 4
}

public sealed record RemoteSceneDiscoveryCallResult(
    bool Succeeded,
    RemoteSceneDiscoveryPageDto? Value,
    RemoteSceneDiscoveryCallErrorKind ErrorKind,
    string? Error)
{
    public static RemoteSceneDiscoveryCallResult Success(RemoteSceneDiscoveryPageDto value) =>
        new(true, value, RemoteSceneDiscoveryCallErrorKind.None, null);

    public static RemoteSceneDiscoveryCallResult Validation(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.Validation, error);

    public static RemoteSceneDiscoveryCallResult Timeout(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.Timeout, error);

    public static RemoteSceneDiscoveryCallResult Unavailable(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.Unavailable, error);

    public static RemoteSceneDiscoveryCallResult InvalidPayload(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.InvalidPayload, error);
}
