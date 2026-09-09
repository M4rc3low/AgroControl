namespace AgroControl.Application.Sync;

public sealed record OfflineStoredOperation(
    Guid OrganizationId,
    Guid UserId,
    Guid OperationId,
    Guid FarmId,
    string EntityKind,
    Guid EntityId,
    string OperationType,
    string RequestHash,
    string Status,
    string? ResultJson,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime ExpiresAtUtc);

public interface IOfflineSyncIdempotencyRepository
{
    Task<bool> TryClaimAsync(
        Guid organizationId,
        Guid userId,
        Guid operationId,
        Guid farmId,
        string entityKind,
        Guid entityId,
        string operationType,
        string requestHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<OfflineStoredOperation?> GetAsync(
        Guid organizationId,
        Guid userId,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid organizationId,
        Guid userId,
        Guid operationId,
        string requestHash,
        string resultJson,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default);
}

public interface IOfflineSyncTransaction
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
