namespace AgroControl.Application.PrecisionAgriculture;

public sealed record GeoJsonPolygonDto(string Type, double[][][] Coordinates);

public sealed record PrecisionFieldDto(
    Guid FieldId,
    Guid FarmId,
    string Name,
    decimal RegisteredAreaHectares,
    bool IsActive,
    bool HasBoundary,
    GeoJsonPolygonDto? Boundary,
    decimal? SpatialAreaHectares,
    decimal? AreaDifferenceHectares,
    decimal? AreaDifferencePercent);

public sealed record SpatialFieldSnapshot(
    Guid FieldId,
    Guid FarmId,
    string Name,
    decimal RegisteredAreaHectares,
    bool IsActive,
    string? BoundaryGeoJson,
    decimal? SpatialAreaHectares);

public sealed record GeoJsonValidationResult(bool Succeeded, GeoJsonPolygonDto? Value, string? Error)
{
    public static GeoJsonValidationResult Success(GeoJsonPolygonDto value) => new(true, value, null);
    public static GeoJsonValidationResult Failure(string error) => new(false, null, error);
}
