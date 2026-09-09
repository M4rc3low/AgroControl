namespace AgroControl.Application.Sync;

public sealed record OfflineSyncChangeEntry(
    long Sequence,
    Guid OrganizationId,
    Guid? FarmId,
    string EntityKind,
    Guid EntityId,
    string ChangeType,
    DateTime OccurredAtUtc);

public interface IOfflineSyncChangeRepository
{
    Task<long> GetCurrentSequenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OfflineSyncChangeEntry>> ListAfterAsync(
        Guid organizationId,
        Guid farmId,
        long afterSequence,
        int take,
        CancellationToken cancellationToken = default);
}
