namespace AgroControl.Application.PrecisionAgriculture;

public interface IPrecisionAgricultureRepository
{
    Task<IReadOnlyList<SpatialFieldSnapshot>> ListFieldsAsync(Guid organizationId, Guid? farmId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<SpatialFieldSnapshot?> GetFieldAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default);
    Task<SpatialFieldSnapshot?> UpsertBoundaryAsync(Guid organizationId, Guid fieldId, string geoJson, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
    Task<SpatialFieldSnapshot?> ClearBoundaryAsync(Guid organizationId, Guid fieldId, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagementZoneSnapshot>> ListZonesAsync(Guid organizationId, Guid? fieldId, string? type, string? classification, bool includeInactive, CancellationToken cancellationToken = default);
    Task<ManagementZoneSnapshot?> GetZoneAsync(Guid organizationId, Guid zoneId, CancellationToken cancellationToken = default);
    Task<ZoneGeometryValidationSnapshot?> ValidateZoneGeometryAsync(Guid organizationId, Guid fieldId, string geoJson, CancellationToken cancellationToken = default);
    Task<ManagementZoneSnapshot?> AddZoneAsync(Guid organizationId, ManagementZoneWriteModel model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagementZoneSnapshot>?> AddZonesAtomicallyAsync(Guid organizationId, IReadOnlyList<ManagementZoneWriteModel> models, CancellationToken cancellationToken = default);
    Task<ManagementZoneSnapshot?> UpdateZoneAsync(Guid organizationId, ManagementZoneWriteModel model, CancellationToken cancellationToken = default);
    Task<ManagementZoneSnapshot?> DeactivateZoneAsync(Guid organizationId, Guid zoneId, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}
