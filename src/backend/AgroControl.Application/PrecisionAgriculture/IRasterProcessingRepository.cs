namespace AgroControl.Application.PrecisionAgriculture;

public interface IRasterProcessingRepository
{
    Task<RasterProcessingRunSnapshot?> GetRunByKeyAsync(Guid organizationId, string processingKey, CancellationToken cancellationToken = default);
    Task<RasterRunCreationResult> CreateRunAsync(Guid organizationId, RasterProductWriteModel product, RasterProcessingRunWriteModel run, CancellationToken cancellationToken = default);
    Task<RasterProcessingRunSnapshot?> MarkProcessingAsync(Guid organizationId, Guid runId, DateTime startedAtUtc, CancellationToken cancellationToken = default);
    Task<RasterProcessingRunSnapshot?> MarkFailedAsync(Guid organizationId, Guid runId, string failureMessage, DateTime completedAtUtc, CancellationToken cancellationToken = default);
    Task<RasterProcessingRunSnapshot?> CompleteAsync(Guid organizationId, Guid runId, RasterCompletionMetadata metadata,
        IReadOnlyList<RasterZonalResultWriteModel> results, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RasterProcessingRunSnapshot>> ListRunsAsync(Guid organizationId, Guid sceneId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RasterZonalResultSnapshot>> ListResultsByRunAsync(Guid organizationId, Guid runId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<RasterZonalResultSnapshot> Items, int TotalCount)> ListResultsAsync(Guid organizationId, int skip, int take,
        Guid? fieldId, Guid? seasonId, Guid? managementZoneId, string? indexType, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default);
}
