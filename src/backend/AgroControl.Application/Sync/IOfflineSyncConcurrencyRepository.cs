namespace AgroControl.Application.Sync;

public sealed record OfflineSyncEntityVersion(DateTime UpdatedAtUtc);

public interface IOfflineSyncConcurrencyRepository
{
    Task<OfflineSyncEntityVersion?> LockFieldAsync(
        Guid organizationId,
        Guid farmId,
        Guid fieldId,
        CancellationToken cancellationToken = default);

    Task<OfflineSyncEntityVersion?> LockSeasonAsync(
        Guid organizationId,
        Guid farmId,
        Guid seasonId,
        CancellationToken cancellationToken = default);
}
