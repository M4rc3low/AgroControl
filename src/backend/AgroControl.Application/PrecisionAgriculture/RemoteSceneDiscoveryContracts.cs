namespace AgroControl.Application.PrecisionAgriculture;

public sealed record RemoteSceneDiscoveryProviderDto(
    string Key,
    string DisplayName,
    bool IsEnabled,
    IReadOnlyList<string> Collections);

public sealed record SearchRemoteScenesCommand(
    Guid FieldId,
    Guid? SeasonId,
    string Provider,
    DateTime FromUtc,
    DateTime ToUtc,
    int PageSize = 30,
    string? Collection = null,
    decimal? MaxCloudCoveragePercent = null,
    string? ContinuationToken = null);

public sealed record ImportRemoteSceneCommand(
    Guid FieldId,
    Guid? SeasonId,
    string Provider,
    string Collection,
    string ExternalId,
    string AssetKey);

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

public sealed record RemoteSceneDiscoveryItemCallResult(
    bool Succeeded,
    RemoteSceneDiscoveryItemDto? Value,
    RemoteSceneDiscoveryCallErrorKind ErrorKind,
    string? Error)
{
    public static RemoteSceneDiscoveryItemCallResult Success(RemoteSceneDiscoveryItemDto value) =>
        new(true, value, RemoteSceneDiscoveryCallErrorKind.None, null);

    public static RemoteSceneDiscoveryItemCallResult Validation(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.Validation, error);

    public static RemoteSceneDiscoveryItemCallResult Timeout(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.Timeout, error);

    public static RemoteSceneDiscoveryItemCallResult Unavailable(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.Unavailable, error);

    public static RemoteSceneDiscoveryItemCallResult InvalidPayload(string error) =>
        new(false, null, RemoteSceneDiscoveryCallErrorKind.InvalidPayload, error);
}

public enum RemoteSceneDiscoveryResultKind
{
    Success = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Timeout = 4,
    Unavailable = 5,
    InvalidPayload = 6
}

public sealed record RemoteSceneDiscoverySearchResult(
    RemoteSceneDiscoveryResultKind Kind,
    RemoteSceneDiscoveryPageDto? Value,
    string? Error)
{
    public static RemoteSceneDiscoverySearchResult Success(RemoteSceneDiscoveryPageDto value) =>
        new(RemoteSceneDiscoveryResultKind.Success, value, null);

    public static RemoteSceneDiscoverySearchResult Validation(string error) =>
        new(RemoteSceneDiscoveryResultKind.Validation, null, error);

    public static RemoteSceneDiscoverySearchResult NotFound(string error) =>
        new(RemoteSceneDiscoveryResultKind.NotFound, null, error);

    public static RemoteSceneDiscoverySearchResult Conflict(string error) =>
        new(RemoteSceneDiscoveryResultKind.Conflict, null, error);

    public static RemoteSceneDiscoverySearchResult Timeout(string error) =>
        new(RemoteSceneDiscoveryResultKind.Timeout, null, error);

    public static RemoteSceneDiscoverySearchResult Unavailable(string error) =>
        new(RemoteSceneDiscoveryResultKind.Unavailable, null, error);

    public static RemoteSceneDiscoverySearchResult InvalidPayload(string error) =>
        new(RemoteSceneDiscoveryResultKind.InvalidPayload, null, error);
}

public sealed record RemoteSceneDiscoveryImportResult(
    RemoteSceneDiscoveryResultKind Kind,
    RemoteSensingSceneDto? Value,
    string? Error)
{
    public static RemoteSceneDiscoveryImportResult Success(RemoteSensingSceneDto value) =>
        new(RemoteSceneDiscoveryResultKind.Success, value, null);

    public static RemoteSceneDiscoveryImportResult Validation(string error) =>
        new(RemoteSceneDiscoveryResultKind.Validation, null, error);

    public static RemoteSceneDiscoveryImportResult NotFound(string error) =>
        new(RemoteSceneDiscoveryResultKind.NotFound, null, error);

    public static RemoteSceneDiscoveryImportResult Conflict(string error) =>
        new(RemoteSceneDiscoveryResultKind.Conflict, null, error);

    public static RemoteSceneDiscoveryImportResult Timeout(string error) =>
        new(RemoteSceneDiscoveryResultKind.Timeout, null, error);

    public static RemoteSceneDiscoveryImportResult Unavailable(string error) =>
        new(RemoteSceneDiscoveryResultKind.Unavailable, null, error);

    public static RemoteSceneDiscoveryImportResult InvalidPayload(string error) =>
        new(RemoteSceneDiscoveryResultKind.InvalidPayload, null, error);
}
