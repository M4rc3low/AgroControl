namespace AgroControl.Application.PrecisionAgriculture;

public sealed record CreateRemoteSensingSceneCommand(
    Guid FieldId,
    Guid? SeasonId,
    string Provider,
    string ExternalId,
    string Platform,
    DateTime AcquiredAtUtc,
    decimal? CloudCoveragePercent,
    decimal? SpatialResolutionMeters,
    string? AssetReference,
    string? Notes,
    GeoJsonPolygonDto? Footprint);

public sealed record UpdateRemoteSensingSceneCommand(
    Guid FieldId,
    Guid? SeasonId,
    string Platform,
    DateTime AcquiredAtUtc,
    decimal? CloudCoveragePercent,
    decimal? SpatialResolutionMeters,
    string? AssetReference,
    string? Notes,
    GeoJsonPolygonDto? Footprint);

public sealed record RemoteSensingSceneDto(
    Guid Id,
    Guid FieldId,
    Guid? SeasonId,
    string Provider,
    string ExternalId,
    string Platform,
    DateTime AcquiredAtUtc,
    decimal? CloudCoveragePercent,
    decimal? SpatialResolutionMeters,
    string? AssetReference,
    string? Notes,
    GeoJsonPolygonDto? Footprint,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record RemoteSensingSceneSnapshot(
    Guid Id,
    Guid FieldId,
    Guid? SeasonId,
    string Provider,
    string ExternalId,
    string Platform,
    DateTime AcquiredAtUtc,
    decimal? CloudCoveragePercent,
    decimal? SpatialResolutionMeters,
    string? AssetReference,
    string? Notes,
    string? FootprintGeoJson,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record RemoteSensingSceneWriteModel(
    Guid Id,
    Guid FieldId,
    Guid? SeasonId,
    string Provider,
    string ExternalId,
    string Platform,
    DateTime AcquiredAtUtc,
    decimal? CloudCoveragePercent,
    decimal? SpatialResolutionMeters,
    string? AssetReference,
    string? Notes,
    string? FootprintGeoJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateVegetationIndexObservationCommand(
    Guid SceneId,
    Guid? ManagementZoneId,
    string IndexType,
    string? CustomIndexName,
    decimal Minimum,
    decimal Maximum,
    decimal Mean,
    decimal Median,
    decimal StandardDeviation,
    decimal ValidCoveragePercent,
    long? SampleCount);

public sealed record VegetationIndexObservationDto(
    Guid Id,
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
    long? SampleCount,
    string Source,
    string Platform,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc);

public sealed record VegetationIndexObservationSnapshot(
    Guid Id,
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
    long? SampleCount,
    string Source,
    string Platform,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc);

public sealed record VegetationIndexObservationWriteModel(
    Guid Id,
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
    long? SampleCount,
    string Source,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc);

public sealed record VegetationIndexSeriesPointDto(
    Guid ObservationId,
    Guid SceneId,
    Guid? ManagementZoneId,
    string IndexType,
    string? CustomIndexName,
    decimal Mean,
    decimal Minimum,
    decimal Maximum,
    decimal Median,
    decimal StandardDeviation,
    decimal ValidCoveragePercent,
    long? SampleCount,
    string Source,
    string Platform,
    DateTime ObservedAtUtc);

public sealed record RemoteSensingLatestMetricDto(
    string IndexType,
    string? CustomIndexName,
    decimal Mean,
    decimal Minimum,
    decimal Maximum,
    decimal ValidCoveragePercent,
    string Source,
    string Platform,
    DateTime ObservedAtUtc,
    Guid SceneId,
    Guid? ManagementZoneId);

public sealed record RemoteSensingSummaryDto(
    Guid FieldId,
    Guid? SeasonId,
    Guid? ManagementZoneId,
    int SceneCount,
    IReadOnlyList<RemoteSensingLatestMetricDto> LatestMetrics);
