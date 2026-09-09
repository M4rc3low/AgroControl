using System.Data;
using System.Data.Common;
using AgroControl.Application.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgroControl.Infrastructure.Persistence;

public sealed class OfflineSyncIdempotencyRepository(AgroControlDbContext dbContext)
    : IOfflineSyncIdempotencyRepository
{
    public async Task<bool> TryClaimAsync(
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
        CancellationToken cancellationToken = default)
    {
        return await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection);
            command.CommandText =
                """
                INSERT INTO offline_sync_operations (
                    organization_id,
                    user_id,
                    operation_id,
                    farm_id,
                    entity_kind,
                    entity_id,
                    operation_type,
                    request_hash,
                    status,
                    result_json,
                    created_at_utc,
                    completed_at_utc,
                    expires_at_utc)
                VALUES (
                    @organization_id,
                    @user_id,
                    @operation_id,
                    @farm_id,
                    @entity_kind,
                    @entity_id,
                    @operation_type,
                    @request_hash,
                    'processing',
                    NULL,
                    @created_at_utc,
                    NULL,
                    @expires_at_utc)
                ON CONFLICT (organization_id, user_id, operation_id) DO NOTHING;
                """;
            AddParameter(command, "@organization_id", organizationId);
            AddParameter(command, "@user_id", userId);
            AddParameter(command, "@operation_id", operationId);
            AddParameter(command, "@farm_id", farmId);
            AddParameter(command, "@entity_kind", entityKind);
            AddParameter(command, "@entity_id", entityId);
            AddParameter(command, "@operation_type", operationType);
            AddParameter(command, "@request_hash", requestHash);
            AddParameter(command, "@created_at_utc", createdAtUtc);
            AddParameter(command, "@expires_at_utc", expiresAtUtc);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }, cancellationToken);
    }

    public async Task<OfflineStoredOperation?> GetAsync(
        Guid organizationId,
        Guid userId,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        return await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection);
            command.CommandText =
                """
                SELECT organization_id,
                       user_id,
                       operation_id,
                       farm_id,
                       entity_kind,
                       entity_id,
                       operation_type,
                       request_hash,
                       status,
                       result_json::text,
                       created_at_utc,
                       completed_at_utc,
                       expires_at_utc
                  FROM offline_sync_operations
                 WHERE organization_id = @organization_id
                   AND user_id = @user_id
                   AND operation_id = @operation_id;
                """;
            AddParameter(command, "@organization_id", organizationId);
            AddParameter(command, "@user_id", userId);
            AddParameter(command, "@operation_id", operationId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            return new OfflineStoredOperation(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetGuid(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetDateTime(10),
                reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                reader.GetDateTime(12));
        }, cancellationToken);
    }

    public async Task CompleteAsync(
        Guid organizationId,
        Guid userId,
        Guid operationId,
        string requestHash,
        string resultJson,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var updated = await WithConnectionAsync(async connection =>
        {
            await using var command = CreateCommand(connection);
            command.CommandText =
                """
                UPDATE offline_sync_operations
                   SET status = 'completed',
                       result_json = CAST(@result_json AS jsonb),
                       completed_at_utc = @completed_at_utc
                 WHERE organization_id = @organization_id
                   AND user_id = @user_id
                   AND operation_id = @operation_id
                   AND request_hash = @request_hash
                   AND status = 'processing';
                """;
            AddParameter(command, "@result_json", resultJson);
            AddParameter(command, "@completed_at_utc", completedAtUtc);
            AddParameter(command, "@organization_id", organizationId);
            AddParameter(command, "@user_id", userId);
            AddParameter(command, "@operation_id", operationId);
            AddParameter(command, "@request_hash", requestHash);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);

        if (updated != 1)
            throw new InvalidOperationException("Offline sync idempotency operation could not be completed atomically.");
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        var currentTransaction = dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
            command.Transaction = currentTransaction.GetDbTransaction();
        return command;
    }

    private async Task<T> WithConnectionAsync<T>(
        Func<DbConnection, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);

        try
        {
            return await action(connection);
        }
        finally
        {
            if (shouldClose && dbContext.Database.CurrentTransaction is null)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
