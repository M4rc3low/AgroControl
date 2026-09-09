using System.Data;
using System.Data.Common;
using AgroControl.Application.Sync;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class OfflineSyncChangeRepository(AgroControlDbContext dbContext) : IOfflineSyncChangeRepository
{
    public async Task<long> GetCurrentSequenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await WithConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COALESCE(MAX(sequence), 0) FROM offline_sync_changes WHERE organization_id = @organization_id;";
            AddParameter(command, "@organization_id", organizationId);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null or DBNull ? 0L : Convert.ToInt64(result);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<OfflineSyncChangeEntry>> ListAfterAsync(
        Guid organizationId,
        Guid farmId,
        long afterSequence,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (afterSequence < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence));
        take = Math.Clamp(take, 1, 501);

        return await WithConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT c.sequence,
                       c.organization_id,
                       c.farm_id,
                       c.entity_kind,
                       c.entity_id,
                       c.change_type,
                       c.occurred_at_utc
                  FROM offline_sync_changes AS c
                 WHERE c.organization_id = @organization_id
                   AND c.sequence > @after_sequence
                   AND (
                        c.farm_id = @farm_id
                        OR (
                            c.farm_id IS NULL
                            AND c.entity_kind = 'crop'
                            AND c.change_type = 'upsert'
                            AND EXISTS (
                                SELECT 1
                                  FROM seasons AS season
                                  JOIN fields AS field ON field."Id" = season."FieldId"
                                 WHERE season."OrganizationId" = @organization_id
                                   AND season."CropId" = c.entity_id
                                   AND field."FarmId" = @farm_id
                            )
                        )
                   )
                 ORDER BY c.sequence
                 LIMIT @take;
                """;
            AddParameter(command, "@organization_id", organizationId);
            AddParameter(command, "@farm_id", farmId);
            AddParameter(command, "@after_sequence", afterSequence);
            AddParameter(command, "@take", take);

            var items = new List<OfflineSyncChangeEntry>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new OfflineSyncChangeEntry(
                    reader.GetInt64(0),
                    reader.GetGuid(1),
                    reader.IsDBNull(2) ? null : reader.GetGuid(2),
                    reader.GetString(3),
                    reader.GetGuid(4),
                    reader.GetString(5),
                    reader.GetDateTime(6)));
            }

            return (IReadOnlyList<OfflineSyncChangeEntry>)items;
        }, cancellationToken);
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
            if (shouldClose) await connection.CloseAsync();
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
