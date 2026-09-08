namespace AgroControl.Application.PrecisionAgriculture;

public sealed record ProcessRasterSceneCommand(
    string ProductType,
    string? CustomProductName,
    string? AssetReference,
    int Band = 1,
    bool IncludeManagementZones = true,
    string? ProcessingKey = null);

public sealed record RasterProductWriteModel(
    Guid Id,
    Guid SceneId,
    string ProductType,
    string? CustomProductName,
    string AssetReference,
    int Band,
    DateTime CreatedAtUtc);

public sealed record RasterProcessingRunWriteModel(
    Guid Id,
    Guid ProductId,
    Guid SceneId,
    string ProcessingKey,
    bool IncludeManagementZones,
    DateTime RequestedAtUtc);

public sealed record RasterProcessingRunSnapshot(
    Guid Id,
    Guid ProductId,
    Guid SceneId,
    string ProcessingKey,
    string Status,
    bool IncludeManagementZones,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? FailureMessage,
    string ProductType,
    string? CustomProductName,
    string AssetReference,
    int Band,
    string? Crs,
    decimal? ResolutionX,
    decimal? ResolutionY,
    decimal? Nodata,
    int? Width,
    int? Height);

public sealed record RasterProcessingRunDto(
    Guid Id,
    Guid ProductId,
    Guid SceneId,
    string ProcessingKey,
    string Status,
    bool IncludeManagementZones,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? FailureMessage,
    string ProductType,
    string? CustomProductName,
    string AssetReference,
    int Band,
    string? Crs,
    decimal? ResolutionX,
    decimal? ResolutionY,
    decimal? Nodata,
    int? Width,
    int? Height);

public sealed record RasterZonalResultWriteModel(
    Guid Id,
    Guid ObservationId,
    Guid ProductId,
    Guid SceneId,
    Guid FieldId,
    Guid? SeasonId,
    Guid? ManagementZoneId,
    string IndexType,
    string? CustomIndexName,
    decimal Minimum,
    decimal Maximum,
    decimal Mean,
    decimal Median,
    decimal StandardDeviation,
    decimal ValidCoveragePercent,
    long SampleCount,
    string Source,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc);

public sealed record RasterZonalResultSnapshot(
    Guid Id,
    Guid RunId,
    Guid ProductId,
    Guid ObservationId,
    Guid SceneId,
    Guid FieldId,
    Guid? SeasonId,
    Guid? ManagementZoneId,
    string IndexType,
    string? CustomIndexName,
    decimal Minimum,
    decimal Maximum,
    decimal Mean,
    decimal Median,
    decimal StandardDeviation,
    decimal ValidCoveragePercent,
    long SampleCount,
    string Source,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc);

public sealed record RasterZonalResultDto(
    Guid Id,
    Guid RunId,
    Guid ProductId,
    Guid ObservationId,
    Guid SceneId,
    Guid FieldId,
    Guid? SeasonId,
    Guid? ManagementZoneId,
    string IndexType,
    string? CustomIndexName,
    decimal Minimum,
    decimal Maximum,
    decimal Mean,
    decimal Median,
    decimal StandardDeviation,
    decimal ValidCoveragePercent,
    long SampleCount,
    string Source,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc);

public sealed record RasterCompletionMetadata(
    string Crs,
    decimal ResolutionX,
    decimal ResolutionY,
    decimal? Nodata,
    int Width,
    int Height);

public sealed record RasterRunCreationResult(RasterProcessingRunSnapshot Run, bool Created);

public sealed record RasterProcessingResponseDto(
    RasterProcessingRunDto Run,
    bool Reused,
    IReadOnlyList<RasterZonalResultDto> Results);

public enum RasterProcessResultKind
{
    Success,
    Validation,
    NotFound,
    Conflict,
    Timeout,
    Unavailable
}

public sealed record RasterProcessResult(
    RasterProcessResultKind Kind,
    RasterProcessingResponseDto? Value,
    string? Error)
{
    public bool Succeeded => Kind == RasterProcessResultKind.Success;
    public static RasterProcessResult Success(RasterProcessingResponseDto value) => new(RasterProcessResultKind.Success, value, null);
    public static RasterProcessResult Validation(string error) => new(RasterProcessResultKind.Validation, null, error);
    public static RasterProcessResult NotFound(string error) => new(RasterProcessResultKind.NotFound, null, error);
    public static RasterProcessResult Conflict(string error) => new(RasterProcessResultKind.Conflict, null, error);
    public static RasterProcessResult Timeout(string error) => new(RasterProcessResultKind.Timeout, null, error);
    public static RasterProcessResult Unavailable(string error) => new(RasterProcessResultKind.Unavailable, null, error);
}
