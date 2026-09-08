using System.Text.Json;
using AgroControl.Application.Common;

namespace AgroControl.Application.PrecisionAgriculture;

public sealed class PrecisionAgricultureService(IPrecisionAgricultureRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PrecisionFieldDto>> ListFieldsAsync(Guid organizationId, Guid? farmId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var items = await repository.ListFieldsAsync(organizationId, farmId, includeInactive, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<PrecisionFieldDto?> GetFieldAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<OperationResult<PrecisionFieldDto>> UpsertBoundaryAsync(Guid organizationId, Guid fieldId, GeoJsonPolygonDto? polygon, CancellationToken cancellationToken = default)
    {
        var current = await repository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        if (current is null)
            return OperationResult<PrecisionFieldDto>.NotFound("Field was not found in the current organization.");
        if (!current.IsActive)
            return OperationResult<PrecisionFieldDto>.Conflict("Inactive fields cannot receive a new geographic boundary.");

        var validation = GeoJsonPolygonValidator.ValidateAndNormalize(polygon);
        if (!validation.Succeeded)
            return OperationResult<PrecisionFieldDto>.Validation(validation.Error ?? "Invalid GeoJSON polygon.");

        var geoJson = JsonSerializer.Serialize(validation.Value, JsonOptions);
        var updated = await repository.UpsertBoundaryAsync(organizationId, fieldId, geoJson, DateTime.UtcNow, cancellationToken);
        if (updated is null)
            return OperationResult<PrecisionFieldDto>.Validation("The polygon is topologically invalid according to PostGIS.");

        return OperationResult<PrecisionFieldDto>.Success(ToDto(updated));
    }

    public async Task<OperationResult<PrecisionFieldDto>> DeleteBoundaryAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        var current = await repository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        if (current is null)
            return OperationResult<PrecisionFieldDto>.NotFound("Field was not found in the current organization.");

        var updated = await repository.ClearBoundaryAsync(organizationId, fieldId, DateTime.UtcNow, cancellationToken);
        return updated is null
            ? OperationResult<PrecisionFieldDto>.NotFound("Field was not found in the current organization.")
            : OperationResult<PrecisionFieldDto>.Success(ToDto(updated));
    }

    private static PrecisionFieldDto ToDto(SpatialFieldSnapshot item)
    {
        GeoJsonPolygonDto? boundary = null;
        if (!string.IsNullOrWhiteSpace(item.BoundaryGeoJson))
            boundary = JsonSerializer.Deserialize<GeoJsonPolygonDto>(item.BoundaryGeoJson, JsonOptions);

        decimal? difference = null;
        decimal? differencePercent = null;
        if (item.SpatialAreaHectares is { } spatialArea)
        {
            difference = decimal.Round(spatialArea - item.RegisteredAreaHectares, 4, MidpointRounding.AwayFromZero);
            if (item.RegisteredAreaHectares != 0)
                differencePercent = decimal.Round(difference.Value / item.RegisteredAreaHectares * 100m, 2, MidpointRounding.AwayFromZero);
        }

        return new PrecisionFieldDto(item.FieldId, item.FarmId, item.Name, item.RegisteredAreaHectares, item.IsActive, boundary is not null, boundary, item.SpatialAreaHectares, difference, differencePercent);
    }
}
