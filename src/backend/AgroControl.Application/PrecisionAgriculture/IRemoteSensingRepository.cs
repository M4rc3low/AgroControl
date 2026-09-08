namespace AgroControl.Application.PrecisionAgriculture;

public interface IRemoteSensingRepository
{
    Task<(IReadOnlyList<RemoteSensingSceneSnapshot> Items, int TotalCount)> ListScenesAsync(
        Guid organizationId, int skip, int take, Guid? fieldId, Guid? seasonId, string? platform, string? provider,
        DateTime? fromUtc, DateTime? toUtc, bool includeInactive, CancellationToken cancellationToken = default);

    Task<RemoteSensingSceneSnapshot?> GetSceneAsync(Guid organizationId, Guid sceneId, CancellationToken cancellationToken = default);
    Task<RemoteSensingSceneSnapshot?> AddSceneAsync(Guid organizationId, RemoteSensingSceneWriteModel model, CancellationToken cancellationToken = default);
    Task<RemoteSensingSceneSnapshot?> UpdateSceneAsync(Guid organizationId, RemoteSensingSceneWriteModel model, CancellationToken cancellationToken = default);
    Task<RemoteSensingSceneSnapshot?> DeactivateSceneAsync(Guid organizationId, Guid sceneId, DateTime updatedAtUtc, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VegetationIndexObservationSnapshot> Items, int TotalCount)> ListObservationsAsync(
        Guid organizationId, int skip, int take, Guid? sceneId, Guid? fieldId, Guid? seasonId, Guid? managementZoneId,
        string? indexType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    Task<VegetationIndexObservationSnapshot?> AddObservationAsync(Guid organizationId, VegetationIndexObservationWriteModel model, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VegetationIndexObservationSnapshot>> ListSeriesAsync(
        Guid organizationId, Guid fieldId, Guid? seasonId, Guid? managementZoneId, string? indexType,
        DateTime? fromUtc, DateTime? toUtc, int take, CancellationToken cancellationToken = default);

    Task<int> CountScenesAsync(Guid organizationId, Guid fieldId, Guid? seasonId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
