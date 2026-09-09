using System.Data;
using System.Data.Common;
using AgroControl.Application.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgroControl.Infrastructure.Persistence;

public sealed class OfflineSyncConcurrencyRepository(AgroControlDbContext dbContext)
    : IOfflineSyncConcurrencyRepository
{
    public Task<OfflineSyncEntityVersion?> LockFieldAsync(
        Guid organizationId,
        Guid farmId,
        Guid fieldId,
        CancellationToken cancellationToken = default) =>
        LockAsync(
            """
            SELECT field."UpdatedAtUtc"
              FROM fields AS field
             WHERE field."OrganizationId" = @organization_id
               AND field."FarmId" = @farm_id
               AND field."Id" = @entity_id
             FOR UPDATE;
            """,
            organizationId,
            farmId,
            fieldId,
            cancellationToken);

    public Task<OfflineSyncEntityVersion?> LockSeasonAsync(
        Guid organizationId,
        Guid farmId,
        Guid seasonId,
        CancellationToken cancellationToken = default) =>
        LockAsync(
            """
            SELECT season."UpdatedAtUtc"
              FROM seasons AS season
              JOIN fields AS field ON field."Id" = season."FieldId"
             WHERE season."OrganizationId" = @organization_id
               AND field."FarmId" = @farm_id
               AND season."Id" = @entity_id
             FOR UPDATE OF season;
            """,
            organizationId,
            farmId,
            seasonId,
            cancellationToken);

    private async Task<OfflineSyncEntityVersion?> LockAsync(
        string sql,
        Guid organizationId,
        Guid farmId,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Offline sync row locks require an active database transaction.");

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = sql;
        AddParameter(command, "@organization_id", organizationId);
        AddParameter(command, "@farm_id", farmId);
        AddParameter(command, "@entity_id", entityId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull
            ? null
            : new OfflineSyncEntityVersion(Convert.ToDateTime(result));
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
