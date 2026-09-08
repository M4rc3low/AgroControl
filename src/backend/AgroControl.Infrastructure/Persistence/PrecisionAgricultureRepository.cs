using System.Data;
using System.Data.Common;
using AgroControl.Application.PrecisionAgriculture;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class PrecisionAgricultureRepository(AgroControlDbContext dbContext) : IPrecisionAgricultureRepository
{
    public Task<IReadOnlyList<SpatialFieldSnapshot>> ListFieldsAsync(Guid organizationId, Guid? farmId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive",
                   CASE WHEN f."Boundary" IS NULL THEN NULL ELSE ST_AsGeoJSON(f."Boundary"::geometry, 8, 0) END,
                   CASE WHEN f."Boundary" IS NULL THEN NULL ELSE CAST(ST_Area(f."Boundary") / 10000.0 AS numeric(18,4)) END
            FROM fields AS f
            WHERE f."OrganizationId" = @organizationId
              AND (@farmId IS NULL OR f."FarmId" = @farmId)
              AND (@includeInactive OR f."IsActive")
            ORDER BY f."Name", f."Id";
            """;

        return QueryManyAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId);
            AddParameter(command, "farmId", farmId);
            AddParameter(command, "includeInactive", includeInactive);
        }, cancellationToken);
    }

    public Task<SpatialFieldSnapshot?> GetFieldAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive",
                   CASE WHEN f."Boundary" IS NULL THEN NULL ELSE ST_AsGeoJSON(f."Boundary"::geometry, 8, 0) END,
                   CASE WHEN f."Boundary" IS NULL THEN NULL ELSE CAST(ST_Area(f."Boundary") / 10000.0 AS numeric(18,4)) END
            FROM fields AS f
            WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId;
            """;

        return QuerySingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId);
            AddParameter(command, "fieldId", fieldId);
        }, cancellationToken);
    }

    public Task<SpatialFieldSnapshot?> UpsertBoundaryAsync(Guid organizationId, Guid fieldId, string geoJson, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH candidate AS (
                SELECT ST_SetSRID(ST_GeomFromGeoJSON(@geoJson), 4326) AS geom
            ), updated AS (
                UPDATE fields AS f
                SET "Boundary" = candidate.geom::geography,
                    "UpdatedAtUtc" = @updatedAtUtc
                FROM candidate
                WHERE f."OrganizationId" = @organizationId
                  AND f."Id" = @fieldId
                  AND ST_GeometryType(candidate.geom) = 'ST_Polygon'
                  AND NOT ST_IsEmpty(candidate.geom)
                  AND ST_IsValid(candidate.geom)
                RETURNING f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive", f."Boundary"
            )
            SELECT u."Id", u."FarmId", u."Name", u."AreaHectares", u."IsActive",
                   ST_AsGeoJSON(u."Boundary"::geometry, 8, 0),
                   CAST(ST_Area(u."Boundary") / 10000.0 AS numeric(18,4))
            FROM updated AS u;
            """;

        return QuerySingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId);
            AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "geoJson", geoJson);
            AddParameter(command, "updatedAtUtc", updatedAtUtc);
        }, cancellationToken);
    }

    public Task<SpatialFieldSnapshot?> ClearBoundaryAsync(Guid organizationId, Guid fieldId, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE fields AS f
            SET "Boundary" = NULL,
                "UpdatedAtUtc" = @updatedAtUtc
            WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId
            RETURNING f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive", NULL::text, NULL::numeric;
            """;

        return QuerySingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId);
            AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "updatedAtUtc", updatedAtUtc);
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<SpatialFieldSnapshot>> QueryManyAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<SpatialFieldSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadSnapshot(reader));
            return items;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private async Task<SpatialFieldSnapshot?> QuerySingleAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadSnapshot(reader) : null;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static SpatialFieldSnapshot ReadSnapshot(DbDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetDecimal(3), reader.GetBoolean(4),
        reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6));

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
