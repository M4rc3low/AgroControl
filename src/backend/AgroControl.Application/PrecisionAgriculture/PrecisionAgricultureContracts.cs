namespace AgroControl.Application.PrecisionAgriculture;

public sealed record GeoJsonPolygonDto(string Type, double[][][] Coordinates);

public sealed record PrecisionFieldDto(Guid FieldId, Guid FarmId, string Name, decimal RegisteredAreaHectares, bool IsActive,
    bool HasBoundary, GeoJsonPolygonDto? Boundary, decimal? SpatialAreaHectares, decimal? AreaDifferenceHectares, decimal? AreaDifferencePercent);

public sealed record SpatialFieldSnapshot(Guid FieldId, Guid FarmId, string Name, decimal RegisteredAreaHectares,
    bool IsActive, string? BoundaryGeoJson, decimal? SpatialAreaHectares);

public sealed record GeoJsonValidationResult(bool Succeeded, GeoJsonPolygonDto? Value, string? Error)
{
    public static GeoJsonValidationResult Success(GeoJsonPolygonDto value) => new(true, value, null);
    public static GeoJsonValidationResult Failure(string error) => new(false, null, error);
}

public sealed record CreateManagementZoneCommand(Guid FieldId, string Type, string Name, string? Description,
    string? Classification, decimal? Value, string? Unit, GeoJsonPolygonDto Geometry);

public sealed record UpdateManagementZoneCommand(string Type, string Name, string? Description,
    string? Classification, decimal? Value, string? Unit, GeoJsonPolygonDto Geometry);

public sealed record ManagementZoneDto(Guid Id, Guid FieldId, string Type, string Name, string? Description,
    string? Classification, decimal? Value, string? Unit, GeoJsonPolygonDto Geometry, decimal SpatialAreaHectares,
    bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record ManagementZoneSnapshot(Guid Id, Guid FieldId, string Type, string Name, string? Description,
    string? Classification, decimal? Value, string? Unit, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    string GeometryGeoJson, decimal SpatialAreaHectares);

public sealed record ManagementZoneWriteModel(Guid Id, Guid FieldId, string Type, string Name, string? Description,
    string? Classification, decimal? Value, string? Unit, string GeometryGeoJson, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record ZoneGeometryValidationSnapshot(bool FieldIsActive, bool GeometryIsValid, bool IsWithinFieldBoundary, bool FieldHasBoundary);
public sealed record ManagementZoneImportResult(int ImportedCount, IReadOnlyList<ManagementZoneDto> Items);
public sealed record GeoJsonFeatureDto(string Type, IReadOnlyDictionary<string, object?> Properties, GeoJsonPolygonDto Geometry);
public sealed record GeoJsonFeatureCollectionDto(string Type, IReadOnlyList<GeoJsonFeatureDto> Features);
