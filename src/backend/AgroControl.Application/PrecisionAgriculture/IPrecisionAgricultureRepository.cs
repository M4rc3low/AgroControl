namespace AgroControl.Application.PrecisionAgriculture;

public interface IPrecisionAgricultureRepository
{
    Task<IReadOnlyList<SpatialFieldSnapshot>> ListFieldsAsync(Guid organizationId, Guid? farmId, bool includeInactive, CancellationToken cancellationToken = default);
    Task<SpatialFieldSnapshot?> GetFieldAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default);
    Task<SpatialFieldSnapshot?> UpsertBoundaryAsync(Guid organizationId, Guid fieldId, string geoJson, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
    Task<SpatialFieldSnapshot?> ClearBoundaryAsync(Guid organizationId, Guid fieldId, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}
