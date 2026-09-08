using System.Data;
using System.Data.Common;
using AgroControl.Application.PrecisionAgriculture;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class PrecisionAgricultureRepository(AgroControlDbContext dbContext) : IPrecisionAgricultureRepository
{
    private const decimal BoundaryToleranceMeters = 0.5m;

    public Task<IReadOnlyList<SpatialFieldSnapshot>> ListFieldsAsync(Guid organizationId, Guid? farmId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive",
                   CASE WHEN f."Boundary" IS NULL THEN NULL ELSE ST_AsGeoJSON(f."Boundary"::geometry, 8, 0) END,
                   CASE WHEN f."Boundary" IS NULL THEN NULL ELSE CAST(ST_Area(f."Boundary") / 10000.0 AS numeric(18,4)) END
            FROM fields AS f
            WHERE f."OrganizationId" = @organizationId
              AND (CAST(@farmId AS uuid) IS NULL OR f."FarmId" = CAST(@farmId AS uuid))
              AND (@includeInactive OR f."IsActive")
            ORDER BY f."Name", f."Id";
            """;
        return QueryFieldsManyAsync(sql, command =>
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
        return QueryFieldSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId);
            AddParameter(command, "fieldId", fieldId);
        }, cancellationToken);
    }

    public Task<SpatialFieldSnapshot?> UpsertBoundaryAsync(Guid organizationId, Guid fieldId, string geoJson, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH candidate AS (SELECT ST_SetSRID(ST_GeomFromGeoJSON(@geoJson), 4326) AS geom), updated AS (
                UPDATE fields AS f SET "Boundary" = candidate.geom::geography, "UpdatedAtUtc" = @updatedAtUtc
                FROM candidate
                WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId
                  AND ST_GeometryType(candidate.geom) = 'ST_Polygon' AND NOT ST_IsEmpty(candidate.geom) AND ST_IsValid(candidate.geom)
                RETURNING f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive", f."Boundary")
            SELECT u."Id", u."FarmId", u."Name", u."AreaHectares", u."IsActive", ST_AsGeoJSON(u."Boundary"::geometry, 8, 0),
                   CAST(ST_Area(u."Boundary") / 10000.0 AS numeric(18,4)) FROM updated AS u;
            """;
        return QueryFieldSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "geoJson", geoJson); AddParameter(command, "updatedAtUtc", updatedAtUtc);
        }, cancellationToken);
    }

    public Task<SpatialFieldSnapshot?> ClearBoundaryAsync(Guid organizationId, Guid fieldId, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE fields AS f SET "Boundary" = NULL, "UpdatedAtUtc" = @updatedAtUtc
            WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId
            RETURNING f."Id", f."FarmId", f."Name", f."AreaHectares", f."IsActive", NULL::text, NULL::numeric;
            """;
        return QueryFieldSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId); AddParameter(command, "updatedAtUtc", updatedAtUtc);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ManagementZoneSnapshot>> ListZonesAsync(Guid organizationId, Guid? fieldId, string? type, string? classification, bool includeInactive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT z."Id", z."FieldId", z."Type", z."Name", z."Description", z."Classification", z."Value", z."Unit",
                   z."IsActive", z."CreatedAtUtc", z."UpdatedAtUtc", ST_AsGeoJSON(z."Geometry"::geometry, 8, 0),
                   CAST(ST_Area(z."Geometry") / 10000.0 AS numeric(18,4))
            FROM management_zones AS z
            WHERE z."OrganizationId" = @organizationId
              AND (CAST(@fieldId AS uuid) IS NULL OR z."FieldId" = CAST(@fieldId AS uuid))
              AND (CAST(@type AS text) IS NULL OR z."Type" = CAST(@type AS text))
              AND (CAST(@classification AS text) IS NULL OR LOWER(COALESCE(z."Classification", '')) LIKE '%' || LOWER(CAST(@classification AS text)) || '%')
              AND (@includeInactive OR z."IsActive")
            ORDER BY z."Name", z."Id";
            """;
        return QueryZonesManyAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "type", type); AddParameter(command, "classification", classification); AddParameter(command, "includeInactive", includeInactive);
        }, cancellationToken);
    }

    public Task<ManagementZoneSnapshot?> GetZoneAsync(Guid organizationId, Guid zoneId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT z."Id", z."FieldId", z."Type", z."Name", z."Description", z."Classification", z."Value", z."Unit",
                   z."IsActive", z."CreatedAtUtc", z."UpdatedAtUtc", ST_AsGeoJSON(z."Geometry"::geometry, 8, 0),
                   CAST(ST_Area(z."Geometry") / 10000.0 AS numeric(18,4))
            FROM management_zones AS z WHERE z."OrganizationId" = @organizationId AND z."Id" = @zoneId;
            """;
        return QueryZoneSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "zoneId", zoneId);
        }, null, cancellationToken);
    }

    public async Task<ZoneGeometryValidationSnapshot?> ValidateZoneGeometryAsync(Guid organizationId, Guid fieldId, string geoJson, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH candidate AS (SELECT ST_SetSRID(ST_GeomFromGeoJSON(@geoJson), 4326) AS geom)
            SELECT f."IsActive",
                   (ST_GeometryType(candidate.geom) = 'ST_Polygon' AND NOT ST_IsEmpty(candidate.geom) AND ST_IsValid(candidate.geom)) AS geometry_valid,
                   CASE
                       WHEN ST_GeometryType(candidate.geom) <> 'ST_Polygon' OR ST_IsEmpty(candidate.geom) OR NOT ST_IsValid(candidate.geom) THEN FALSE
                       WHEN f."Boundary" IS NULL THEN TRUE
                       ELSE ST_Covers(ST_Buffer(f."Boundary", @toleranceMeters)::geometry, candidate.geom)
                   END AS within_boundary,
                   (f."Boundary" IS NOT NULL) AS has_boundary
            FROM fields AS f CROSS JOIN candidate
            WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId;
            """;
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql;
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "geoJson", geoJson); AddParameter(command, "toleranceMeters", BoundaryToleranceMeters);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new ZoneGeometryValidationSnapshot(reader.GetBoolean(0), reader.GetBoolean(1), reader.GetBoolean(2), reader.GetBoolean(3))
                : null;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    public Task<ManagementZoneSnapshot?> AddZoneAsync(Guid organizationId, ManagementZoneWriteModel model, CancellationToken cancellationToken = default) =>
        InsertZoneAsync(organizationId, model, null, cancellationToken);

    public async Task<IReadOnlyList<ManagementZoneSnapshot>?> AddZonesAtomicallyAsync(Guid organizationId, IReadOnlyList<ManagementZoneWriteModel> models, CancellationToken cancellationToken = default)
    {
        if (models.Count == 0) return Array.Empty<ManagementZoneSnapshot>();
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var inserted = new List<ManagementZoneSnapshot>(models.Count);
            foreach (var model in models)
            {
                var item = await InsertZoneAsync(organizationId, model, transaction, cancellationToken);
                if (item is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
                inserted.Add(item);
            }
            await transaction.CommitAsync(cancellationToken);
            return inserted;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    public Task<ManagementZoneSnapshot?> UpdateZoneAsync(Guid organizationId, ManagementZoneWriteModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH candidate AS (SELECT ST_SetSRID(ST_GeomFromGeoJSON(@geoJson), 4326) AS geom), eligible AS (
                SELECT f."Id" AS field_id, candidate.geom
                FROM fields AS f CROSS JOIN candidate
                WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId AND f."IsActive"
                  AND ST_GeometryType(candidate.geom) = 'ST_Polygon' AND NOT ST_IsEmpty(candidate.geom) AND ST_IsValid(candidate.geom)
                  AND (f."Boundary" IS NULL OR ST_Covers(ST_Buffer(f."Boundary", @toleranceMeters)::geometry, candidate.geom))
            ), updated AS (
                UPDATE management_zones AS z
                SET "Type" = @type, "Name" = @name, "Description" = @description, "Classification" = @classification,
                    "Value" = @value, "Unit" = @unit, "Geometry" = eligible.geom::geography, "UpdatedAtUtc" = @updatedAtUtc
                FROM eligible
                WHERE z."OrganizationId" = @organizationId AND z."Id" = @id AND z."FieldId" = eligible.field_id AND z."IsActive"
                RETURNING z.*)
            SELECT z."Id", z."FieldId", z."Type", z."Name", z."Description", z."Classification", z."Value", z."Unit",
                   z."IsActive", z."CreatedAtUtc", z."UpdatedAtUtc", ST_AsGeoJSON(z."Geometry"::geometry, 8, 0),
                   CAST(ST_Area(z."Geometry") / 10000.0 AS numeric(18,4)) FROM updated AS z;
            """;
        return QueryZoneSingleAsync(sql, command => ConfigureZoneWrite(command, organizationId, model), null, cancellationToken);
    }

    public Task<ManagementZoneSnapshot?> DeactivateZoneAsync(Guid organizationId, Guid zoneId, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH updated AS (
                UPDATE management_zones AS z SET "IsActive" = FALSE, "UpdatedAtUtc" = @updatedAtUtc
                WHERE z."OrganizationId" = @organizationId AND z."Id" = @zoneId RETURNING z.*)
            SELECT z."Id", z."FieldId", z."Type", z."Name", z."Description", z."Classification", z."Value", z."Unit",
                   z."IsActive", z."CreatedAtUtc", z."UpdatedAtUtc", ST_AsGeoJSON(z."Geometry"::geometry, 8, 0),
                   CAST(ST_Area(z."Geometry") / 10000.0 AS numeric(18,4)) FROM updated AS z;
            """;
        return QueryZoneSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "zoneId", zoneId); AddParameter(command, "updatedAtUtc", updatedAtUtc);
        }, null, cancellationToken);
    }

    private Task<ManagementZoneSnapshot?> InsertZoneAsync(Guid organizationId, ManagementZoneWriteModel model, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH candidate AS (SELECT ST_SetSRID(ST_GeomFromGeoJSON(@geoJson), 4326) AS geom), eligible AS (
                SELECT candidate.geom
                FROM fields AS f CROSS JOIN candidate
                WHERE f."OrganizationId" = @organizationId AND f."Id" = @fieldId AND f."IsActive"
                  AND ST_GeometryType(candidate.geom) = 'ST_Polygon' AND NOT ST_IsEmpty(candidate.geom) AND ST_IsValid(candidate.geom)
                  AND (f."Boundary" IS NULL OR ST_Covers(ST_Buffer(f."Boundary", @toleranceMeters)::geometry, candidate.geom))
            ), inserted AS (
                INSERT INTO management_zones ("Id", "OrganizationId", "FieldId", "Type", "Name", "Description", "Classification", "Value", "Unit", "Geometry", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
                SELECT @id, @organizationId, @fieldId, @type, @name, @description, @classification, @value, @unit, eligible.geom::geography, TRUE, @createdAtUtc, @updatedAtUtc
                FROM eligible RETURNING *)
            SELECT z."Id", z."FieldId", z."Type", z."Name", z."Description", z."Classification", z."Value", z."Unit",
                   z."IsActive", z."CreatedAtUtc", z."UpdatedAtUtc", ST_AsGeoJSON(z."Geometry"::geometry, 8, 0),
                   CAST(ST_Area(z."Geometry") / 10000.0 AS numeric(18,4)) FROM inserted AS z;
            """;
        return QueryZoneSingleAsync(sql, command => ConfigureZoneWrite(command, organizationId, model), transaction, cancellationToken);
    }

    private static void ConfigureZoneWrite(DbCommand command, Guid organizationId, ManagementZoneWriteModel model)
    {
        AddParameter(command, "id", model.Id); AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", model.FieldId);
        AddParameter(command, "type", model.Type); AddParameter(command, "name", model.Name); AddParameter(command, "description", model.Description);
        AddParameter(command, "classification", model.Classification); AddParameter(command, "value", model.Value); AddParameter(command, "unit", model.Unit);
        AddParameter(command, "geoJson", model.GeometryGeoJson); AddParameter(command, "createdAtUtc", model.CreatedAtUtc); AddParameter(command, "updatedAtUtc", model.UpdatedAtUtc);
        AddParameter(command, "toleranceMeters", BoundaryToleranceMeters);
    }

    private async Task<IReadOnlyList<SpatialFieldSnapshot>> QueryFieldsManyAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); var items = new List<SpatialFieldSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadField(reader)); return items;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<SpatialFieldSnapshot?> QueryFieldSingleAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken) ? ReadField(reader) : null;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<IReadOnlyList<ManagementZoneSnapshot>> QueryZonesManyAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); var items = new List<ManagementZoneSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadZone(reader)); return items;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<ManagementZoneSnapshot?> QueryZoneSingleAsync(string sql, Action<DbCommand> configure, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = transaction is null && connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken) ? ReadZone(reader) : null;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private static SpatialFieldSnapshot ReadField(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetDecimal(3), reader.GetBoolean(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6));

    private static ManagementZoneSnapshot ReadZone(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6),
        reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetBoolean(8), reader.GetDateTime(9), reader.GetDateTime(10), reader.GetString(11), reader.GetDecimal(12));

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value ?? DBNull.Value; command.Parameters.Add(parameter);
    }
}
