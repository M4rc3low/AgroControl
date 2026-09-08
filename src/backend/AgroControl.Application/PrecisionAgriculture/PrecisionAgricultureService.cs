using System.Text;
using System.Text.Json;
using AgroControl.Application.Common;
using AgroControl.Domain.Modules.PrecisionAgriculture;

namespace AgroControl.Application.PrecisionAgriculture;

public sealed class PrecisionAgricultureService(IPrecisionAgricultureRepository repository)
{
    private const int MaxImportFeatures = 250;
    private const int MaxImportPayloadBytes = 2 * 1024 * 1024;
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
        if (current is null) return OperationResult<PrecisionFieldDto>.NotFound("Field was not found in the current organization.");
        if (!current.IsActive) return OperationResult<PrecisionFieldDto>.Conflict("Inactive fields cannot receive a new geographic boundary.");

        var validation = GeoJsonPolygonValidator.ValidateAndNormalize(polygon);
        if (!validation.Succeeded) return OperationResult<PrecisionFieldDto>.Validation(validation.Error ?? "Invalid GeoJSON polygon.");

        var geoJson = JsonSerializer.Serialize(validation.Value, JsonOptions);
        var updated = await repository.UpsertBoundaryAsync(organizationId, fieldId, geoJson, DateTime.UtcNow, cancellationToken);
        if (updated is null) return OperationResult<PrecisionFieldDto>.Validation("The polygon is topologically invalid according to PostGIS.");
        return OperationResult<PrecisionFieldDto>.Success(ToDto(updated));
    }

    public async Task<OperationResult<PrecisionFieldDto>> DeleteBoundaryAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        var current = await repository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        if (current is null) return OperationResult<PrecisionFieldDto>.NotFound("Field was not found in the current organization.");
        var updated = await repository.ClearBoundaryAsync(organizationId, fieldId, DateTime.UtcNow, cancellationToken);
        return updated is null
            ? OperationResult<PrecisionFieldDto>.NotFound("Field was not found in the current organization.")
            : OperationResult<PrecisionFieldDto>.Success(ToDto(updated));
    }

    public async Task<IReadOnlyList<ManagementZoneDto>> ListZonesAsync(Guid organizationId, Guid? fieldId, string? type, string? classification, bool includeInactive, CancellationToken cancellationToken = default)
    {
        string? normalizedType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<ManagementZoneType>(type, true, out var parsed) || !Enum.IsDefined(parsed)) return [];
            normalizedType = parsed.ToString();
        }
        var items = await repository.ListZonesAsync(organizationId, fieldId, normalizedType, classification?.Trim(), includeInactive, cancellationToken);
        return items.Select(ToZoneDto).ToList();
    }

    public async Task<ManagementZoneDto?> GetZoneAsync(Guid organizationId, Guid zoneId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetZoneAsync(organizationId, zoneId, cancellationToken);
        return item is null ? null : ToZoneDto(item);
    }

    public async Task<OperationResult<ManagementZoneDto>> CreateZoneAsync(Guid organizationId, CreateManagementZoneCommand command, CancellationToken cancellationToken = default)
    {
        var prepared = await PrepareZoneAsync(organizationId, command.FieldId, command.Type, command.Name, command.Description,
            command.Classification, command.Value, command.Unit, command.Geometry, null, null, cancellationToken);
        if (!prepared.Succeeded) return CopyFailure<ManagementZoneWriteModel, ManagementZoneDto>(prepared);

        var created = await repository.AddZoneAsync(organizationId, prepared.Value!, cancellationToken);
        return created is null
            ? OperationResult<ManagementZoneDto>.Validation("The zone geometry is invalid or falls outside the field boundary.")
            : OperationResult<ManagementZoneDto>.Success(ToZoneDto(created));
    }

    public async Task<OperationResult<ManagementZoneDto>> UpdateZoneAsync(Guid organizationId, Guid zoneId, UpdateManagementZoneCommand command, CancellationToken cancellationToken = default)
    {
        var current = await repository.GetZoneAsync(organizationId, zoneId, cancellationToken);
        if (current is null) return OperationResult<ManagementZoneDto>.NotFound("Management zone was not found in the current organization.");
        if (!current.IsActive) return OperationResult<ManagementZoneDto>.Conflict("Inactive management zones cannot be edited.");

        var prepared = await PrepareZoneAsync(organizationId, current.FieldId, command.Type, command.Name, command.Description,
            command.Classification, command.Value, command.Unit, command.Geometry, current.Id, current.CreatedAtUtc, cancellationToken);
        if (!prepared.Succeeded) return CopyFailure<ManagementZoneWriteModel, ManagementZoneDto>(prepared);

        var updated = await repository.UpdateZoneAsync(organizationId, prepared.Value!, cancellationToken);
        return updated is null
            ? OperationResult<ManagementZoneDto>.Validation("The zone geometry is invalid or falls outside the field boundary.")
            : OperationResult<ManagementZoneDto>.Success(ToZoneDto(updated));
    }

    public async Task<OperationResult<ManagementZoneDto>> DeleteZoneAsync(Guid organizationId, Guid zoneId, CancellationToken cancellationToken = default)
    {
        var current = await repository.GetZoneAsync(organizationId, zoneId, cancellationToken);
        if (current is null) return OperationResult<ManagementZoneDto>.NotFound("Management zone was not found in the current organization.");
        var updated = await repository.DeactivateZoneAsync(organizationId, zoneId, DateTime.UtcNow, cancellationToken);
        return updated is null
            ? OperationResult<ManagementZoneDto>.NotFound("Management zone was not found in the current organization.")
            : OperationResult<ManagementZoneDto>.Success(ToZoneDto(updated));
    }

    public async Task<OperationResult<ManagementZoneImportResult>> ImportZonesAsync(Guid organizationId, Guid fieldId, JsonElement document, CancellationToken cancellationToken = default)
    {
        if (Encoding.UTF8.GetByteCount(document.GetRawText()) > MaxImportPayloadBytes)
            return OperationResult<ManagementZoneImportResult>.Validation("GeoJSON import payload exceeds the 2 MB limit.");

        var field = await repository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        if (field is null) return OperationResult<ManagementZoneImportResult>.NotFound("Field was not found in the current organization.");
        if (!field.IsActive) return OperationResult<ManagementZoneImportResult>.Conflict("Inactive fields cannot receive management zones.");

        var parsed = ParseFeatures(document);
        if (!parsed.Success) return OperationResult<ManagementZoneImportResult>.Validation(parsed.Error!);
        if (parsed.Features!.Count == 0) return OperationResult<ManagementZoneImportResult>.Validation("The FeatureCollection does not contain features.");
        if (parsed.Features.Count > MaxImportFeatures) return OperationResult<ManagementZoneImportResult>.Validation($"A single import can contain at most {MaxImportFeatures} features.");

        var models = new List<ManagementZoneWriteModel>(parsed.Features.Count);
        for (var index = 0; index < parsed.Features.Count; index++)
        {
            var feature = parsed.Features[index];
            var properties = feature.Properties;
            if (!TryGetString(properties, "name", out var name) || string.IsNullOrWhiteSpace(name))
                return OperationResult<ManagementZoneImportResult>.Validation($"Feature {index + 1} must contain a non-empty properties.name.");

            var zoneType = TryGetString(properties, "zoneType", out var typeValue) ? typeValue! : ManagementZoneType.Custom.ToString();
            var description = TryGetString(properties, "description", out var d) ? d : null;
            var classification = TryGetString(properties, "classification", out var c) ? c : null;
            var unit = TryGetString(properties, "unit", out var u) ? u : null;
            decimal? value = null;
            if (TryGetProperty(properties, "value", out var valueElement) && valueElement.ValueKind != JsonValueKind.Null)
            {
                if (valueElement.ValueKind != JsonValueKind.Number || !valueElement.TryGetDecimal(out var parsedValue))
                    return OperationResult<ManagementZoneImportResult>.Validation($"Feature {index + 1} properties.value must be numeric.");
                value = parsedValue;
            }

            var prepared = await PrepareZoneAsync(organizationId, fieldId, zoneType, name!, description, classification, value, unit,
                feature.Geometry, null, null, cancellationToken);
            if (!prepared.Succeeded)
                return FailureFromPrepared<ManagementZoneImportResult>(prepared, $"Feature {index + 1}: ");
            models.Add(prepared.Value!);
        }

        var inserted = await repository.AddZonesAtomicallyAsync(organizationId, models, cancellationToken);
        if (inserted is null)
            return OperationResult<ManagementZoneImportResult>.Validation("Import was rolled back because at least one geometry failed the final PostGIS validation.");

        var items = inserted.Select(ToZoneDto).ToList();
        return OperationResult<ManagementZoneImportResult>.Success(new ManagementZoneImportResult(items.Count, items));
    }

    public async Task<OperationResult<GeoJsonFeatureCollectionDto>> ExportZonesAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        var field = await repository.GetFieldAsync(organizationId, fieldId, cancellationToken);
        if (field is null) return OperationResult<GeoJsonFeatureCollectionDto>.NotFound("Field was not found in the current organization.");
        var zones = await repository.ListZonesAsync(organizationId, fieldId, null, null, false, cancellationToken);
        var features = zones.Select(zone => new GeoJsonFeatureDto("Feature", new Dictionary<string, object?>
        {
            ["zoneId"] = zone.Id,
            ["name"] = zone.Name,
            ["zoneType"] = zone.Type,
            ["description"] = zone.Description,
            ["classification"] = zone.Classification,
            ["value"] = zone.Value,
            ["unit"] = zone.Unit,
            ["areaHectares"] = zone.SpatialAreaHectares
        }, DeserializePolygon(zone.GeometryGeoJson))).ToList();
        return OperationResult<GeoJsonFeatureCollectionDto>.Success(new GeoJsonFeatureCollectionDto("FeatureCollection", features));
    }

    private async Task<OperationResult<ManagementZoneWriteModel>> PrepareZoneAsync(Guid organizationId, Guid fieldId, string type,
        string name, string? description, string? classification, decimal? value, string? unit, GeoJsonPolygonDto geometry,
        Guid? existingId, DateTime? existingCreatedAtUtc, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ManagementZoneType>(type, true, out var zoneType) || !Enum.IsDefined(zoneType))
            return OperationResult<ManagementZoneWriteModel>.Validation("Zone type must be Soil, Yield, Vegetation, Prescription or Custom.");

        var validation = GeoJsonPolygonValidator.ValidateAndNormalize(geometry);
        if (!validation.Succeeded) return OperationResult<ManagementZoneWriteModel>.Validation(validation.Error ?? "Invalid GeoJSON polygon.");

        var now = DateTime.UtcNow;
        ManagementZone zone;
        try
        {
            zone = ManagementZone.Create(organizationId, fieldId, zoneType, name, description, classification, value, unit, existingCreatedAtUtc ?? now);
            if (existingId.HasValue) zone.Update(zoneType, name, description, classification, value, unit, now);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<ManagementZoneWriteModel>.Validation(ex.Message);
        }

        var geoJson = JsonSerializer.Serialize(validation.Value, JsonOptions);
        var spatial = await repository.ValidateZoneGeometryAsync(organizationId, fieldId, geoJson, cancellationToken);
        if (spatial is null) return OperationResult<ManagementZoneWriteModel>.NotFound("Field was not found in the current organization.");
        if (!spatial.FieldIsActive) return OperationResult<ManagementZoneWriteModel>.Conflict("Inactive fields cannot receive management zones.");
        if (!spatial.GeometryIsValid) return OperationResult<ManagementZoneWriteModel>.Validation("The polygon is topologically invalid according to PostGIS.");
        if (!spatial.IsWithinFieldBoundary) return OperationResult<ManagementZoneWriteModel>.Validation("The zone must remain inside the field boundary. A 0.5 meter technical tolerance is applied.");

        var id = existingId ?? zone.Id;
        var createdAt = existingCreatedAtUtc ?? zone.CreatedAtUtc;
        return OperationResult<ManagementZoneWriteModel>.Success(new ManagementZoneWriteModel(id, fieldId, zoneType.ToString(), zone.Name,
            zone.Description, zone.Classification, zone.Value, zone.Unit, geoJson, createdAt, now));
    }

    private static (bool Success, List<ParsedFeature>? Features, string? Error) ParseFeatures(JsonElement document)
    {
        if (document.ValueKind != JsonValueKind.Object) return (false, null, "GeoJSON root must be an object.");
        if (!TryGetString(document, "type", out var type)) return (false, null, "GeoJSON root must contain type.");

        var featureElements = new List<JsonElement>();
        if (string.Equals(type, "Feature", StringComparison.OrdinalIgnoreCase)) featureElements.Add(document);
        else if (string.Equals(type, "FeatureCollection", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetProperty(document, "features", out var features) || features.ValueKind != JsonValueKind.Array)
                return (false, null, "FeatureCollection.features must be an array.");
            featureElements.AddRange(features.EnumerateArray());
        }
        else return (false, null, "GeoJSON import accepts only Feature or FeatureCollection roots.");

        var parsed = new List<ParsedFeature>(featureElements.Count);
        for (var index = 0; index < featureElements.Count; index++)
        {
            var feature = featureElements[index];
            if (feature.ValueKind != JsonValueKind.Object || !TryGetString(feature, "type", out var featureType) || !string.Equals(featureType, "Feature", StringComparison.OrdinalIgnoreCase))
                return (false, null, $"Item {index + 1} is not a GeoJSON Feature.");
            if (!TryGetProperty(feature, "geometry", out var geometryElement) || geometryElement.ValueKind != JsonValueKind.Object)
                return (false, null, $"Feature {index + 1} must contain geometry.");
            GeoJsonPolygonDto? geometry;
            try { geometry = geometryElement.Deserialize<GeoJsonPolygonDto>(JsonOptions); }
            catch (JsonException) { return (false, null, $"Feature {index + 1} geometry is malformed."); }
            if (geometry is null) return (false, null, $"Feature {index + 1} geometry is required.");
            var properties = TryGetProperty(feature, "properties", out var p) && p.ValueKind == JsonValueKind.Object ? p : default;
            parsed.Add(new ParsedFeature(geometry, properties));
        }
        return (true, parsed, null);
    }

    private static bool TryGetString(JsonElement element, string name, out string? value)
    {
        value = null;
        if (!TryGetProperty(element, name, out var property) || property.ValueKind != JsonValueKind.String) return false;
        value = property.GetString();
        return true;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object) return false;
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            value = property.Value;
            return true;
        }
        return false;
    }

    private static PrecisionFieldDto ToDto(SpatialFieldSnapshot item)
    {
        GeoJsonPolygonDto? boundary = null;
        if (!string.IsNullOrWhiteSpace(item.BoundaryGeoJson)) boundary = DeserializePolygon(item.BoundaryGeoJson);
        decimal? difference = null;
        decimal? differencePercent = null;
        if (item.SpatialAreaHectares is { } spatialArea)
        {
            difference = decimal.Round(spatialArea - item.RegisteredAreaHectares, 4, MidpointRounding.AwayFromZero);
            if (item.RegisteredAreaHectares != 0) differencePercent = decimal.Round(difference.Value / item.RegisteredAreaHectares * 100m, 2, MidpointRounding.AwayFromZero);
        }
        return new PrecisionFieldDto(item.FieldId, item.FarmId, item.Name, item.RegisteredAreaHectares, item.IsActive, boundary is not null, boundary, item.SpatialAreaHectares, difference, differencePercent);
    }

    private static ManagementZoneDto ToZoneDto(ManagementZoneSnapshot item) => new(item.Id, item.FieldId, item.Type, item.Name,
        item.Description, item.Classification, item.Value, item.Unit, DeserializePolygon(item.GeometryGeoJson), item.SpatialAreaHectares,
        item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static GeoJsonPolygonDto DeserializePolygon(string geoJson) => JsonSerializer.Deserialize<GeoJsonPolygonDto>(geoJson, JsonOptions)
        ?? throw new InvalidOperationException("Stored management-zone geometry could not be deserialized.");

    private static OperationResult<TOut> CopyFailure<TIn, TOut>(OperationResult<TIn> result) => result.ErrorKind switch
    {
        OperationErrorKind.NotFound => OperationResult<TOut>.NotFound(result.Error ?? "Not found."),
        OperationErrorKind.Conflict => OperationResult<TOut>.Conflict(result.Error ?? "Conflict."),
        _ => OperationResult<TOut>.Validation(result.Error ?? "Validation failed.")
    };

    private static OperationResult<TOut> FailureFromPrepared<TOut>(OperationResult<ManagementZoneWriteModel> result, string prefix) => result.ErrorKind switch
    {
        OperationErrorKind.NotFound => OperationResult<TOut>.NotFound(prefix + (result.Error ?? "Not found.")),
        OperationErrorKind.Conflict => OperationResult<TOut>.Conflict(prefix + (result.Error ?? "Conflict.")),
        _ => OperationResult<TOut>.Validation(prefix + (result.Error ?? "Validation failed."))
    };

    private sealed record ParsedFeature(GeoJsonPolygonDto Geometry, JsonElement Properties);
}
