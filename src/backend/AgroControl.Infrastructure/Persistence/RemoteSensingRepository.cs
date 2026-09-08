using System.Data;
using System.Data.Common;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Application.RegionalOperations;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class RemoteSensingRepository(AgroControlDbContext dbContext, IOperationalScopeContext? operationalScope = null) : IRemoteSensingRepository
{
    public async Task<(IReadOnlyList<RemoteSensingSceneSnapshot> Items, int TotalCount)> ListScenesAsync(
        Guid organizationId, int skip, int take, Guid? fieldId, Guid? seasonId, string? platform, string? provider,
        DateTime? fromUtc, DateTime? toUtc, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var filter = $"""
            FROM remote_sensing_scenes AS s
            WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")}
              AND (CAST(@fieldId AS uuid) IS NULL OR s."FieldId" = CAST(@fieldId AS uuid))
              AND (CAST(@seasonId AS uuid) IS NULL OR s."SeasonId" = CAST(@seasonId AS uuid))
              AND (CAST(@platform AS text) IS NULL OR s."Platform" = CAST(@platform AS text))
              AND (CAST(@provider AS text) IS NULL OR LOWER(s."Provider") LIKE '%' || LOWER(CAST(@provider AS text)) || '%')
              AND (CAST(@fromUtc AS timestamptz) IS NULL OR s."AcquiredAtUtc" >= CAST(@fromUtc AS timestamptz))
              AND (CAST(@toUtc AS timestamptz) IS NULL OR s."AcquiredAtUtc" <= CAST(@toUtc AS timestamptz))
              AND (@includeInactive OR s."IsActive")
            """;
        var countSql = "SELECT COUNT(*) " + filter + ";";
        var listSql = """
            SELECT s."Id", s."FieldId", s."SeasonId", s."Provider", s."ExternalId", s."Platform", s."AcquiredAtUtc",
                   s."CloudCoveragePercent", s."SpatialResolutionMeters", s."AssetReference", s."Notes",
                   CASE WHEN s."Footprint" IS NULL THEN NULL ELSE ST_AsGeoJSON(s."Footprint"::geometry, 8, 0) END,
                   s."IsActive", s."CreatedAtUtc", s."UpdatedAtUtc"
            """ + filter + " ORDER BY s.\"AcquiredAtUtc\" DESC, s.\"Id\" DESC LIMIT @take OFFSET @skip;";

        void Configure(DbCommand command)
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "seasonId", seasonId); AddParameter(command, "platform", platform); AddParameter(command, "provider", provider);
            AddParameter(command, "fromUtc", fromUtc); AddParameter(command, "toUtc", toUtc); AddParameter(command, "includeInactive", includeInactive);
        }

        var total = await ExecuteCountAsync(countSql, Configure, cancellationToken);
        var items = await QueryScenesManyAsync(listSql, command => { Configure(command); AddParameter(command, "take", take); AddParameter(command, "skip", skip); }, cancellationToken);
        return (items, total);
    }

    public Task<RemoteSensingSceneSnapshot?> GetSceneAsync(Guid organizationId, Guid sceneId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT s."Id", s."FieldId", s."SeasonId", s."Provider", s."ExternalId", s."Platform", s."AcquiredAtUtc",
                   s."CloudCoveragePercent", s."SpatialResolutionMeters", s."AssetReference", s."Notes",
                   CASE WHEN s."Footprint" IS NULL THEN NULL ELSE ST_AsGeoJSON(s."Footprint"::geometry, 8, 0) END,
                   s."IsActive", s."CreatedAtUtc", s."UpdatedAtUtc"
            FROM remote_sensing_scenes AS s
            WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")} AND s."Id" = @sceneId;
            """;
        return QuerySceneSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "sceneId", sceneId);
        }, cancellationToken);
    }

    public Task<RemoteSensingSceneSnapshot?> AddSceneAsync(Guid organizationId, RemoteSensingSceneWriteModel model, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            WITH candidate AS (
                SELECT CASE WHEN CAST(@footprint AS text) IS NULL THEN NULL
                            ELSE ST_SetSRID(ST_GeomFromGeoJSON(CAST(@footprint AS text)), 4326) END AS geom
            ), eligible AS (
                SELECT f."Id"
                FROM fields AS f
                LEFT JOIN seasons AS se ON se."Id" = CAST(@seasonId AS uuid)
                CROSS JOIN candidate
                WHERE f."OrganizationId" = @organizationId{FieldScopeSql("f")} AND f."Id" = @fieldId AND f."IsActive"
                  AND (CAST(@seasonId AS uuid) IS NULL OR (se."OrganizationId" = @organizationId AND se."FieldId" = f."Id" AND se."IsActive"))
                  AND (candidate.geom IS NULL OR (ST_GeometryType(candidate.geom) = 'ST_Polygon' AND NOT ST_IsEmpty(candidate.geom) AND ST_IsValid(candidate.geom)))
            ), inserted AS (
                INSERT INTO remote_sensing_scenes
                    ("Id", "OrganizationId", "FieldId", "SeasonId", "Provider", "ExternalId", "Platform", "AcquiredAtUtc",
                     "CloudCoveragePercent", "SpatialResolutionMeters", "AssetReference", "Notes", "Footprint", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
                SELECT @id, @organizationId, @fieldId, CAST(@seasonId AS uuid), @provider, @externalId, @platform, @acquiredAtUtc,
                       @cloudCoveragePercent, @spatialResolutionMeters, @assetReference, @notes,
                       CASE WHEN candidate.geom IS NULL THEN NULL ELSE candidate.geom::geography END,
                       TRUE, @createdAtUtc, @updatedAtUtc
                FROM eligible CROSS JOIN candidate
                ON CONFLICT ("OrganizationId", "Provider", "ExternalId") DO NOTHING
                RETURNING *)
            SELECT i."Id", i."FieldId", i."SeasonId", i."Provider", i."ExternalId", i."Platform", i."AcquiredAtUtc",
                   i."CloudCoveragePercent", i."SpatialResolutionMeters", i."AssetReference", i."Notes",
                   CASE WHEN i."Footprint" IS NULL THEN NULL ELSE ST_AsGeoJSON(i."Footprint"::geometry, 8, 0) END,
                   i."IsActive", i."CreatedAtUtc", i."UpdatedAtUtc"
            FROM inserted AS i;
            """;
        return QuerySceneSingleAsync(sql, command => ConfigureSceneWrite(command, organizationId, model), cancellationToken);
    }

    public Task<RemoteSensingSceneSnapshot?> UpdateSceneAsync(Guid organizationId, RemoteSensingSceneWriteModel model, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            WITH candidate AS (
                SELECT CASE WHEN CAST(@footprint AS text) IS NULL THEN NULL
                            ELSE ST_SetSRID(ST_GeomFromGeoJSON(CAST(@footprint AS text)), 4326) END AS geom
            ), eligible AS (
                SELECT f."Id"
                FROM fields AS f
                LEFT JOIN seasons AS se ON se."Id" = CAST(@seasonId AS uuid)
                CROSS JOIN candidate
                WHERE f."OrganizationId" = @organizationId{FieldScopeSql("f")} AND f."Id" = @fieldId AND f."IsActive"
                  AND (CAST(@seasonId AS uuid) IS NULL OR (se."OrganizationId" = @organizationId AND se."FieldId" = f."Id" AND se."IsActive"))
                  AND (candidate.geom IS NULL OR (ST_GeometryType(candidate.geom) = 'ST_Polygon' AND NOT ST_IsEmpty(candidate.geom) AND ST_IsValid(candidate.geom)))
            ), updated AS (
                UPDATE remote_sensing_scenes AS s
                SET "FieldId" = @fieldId, "SeasonId" = CAST(@seasonId AS uuid), "Platform" = @platform,
                    "AcquiredAtUtc" = @acquiredAtUtc, "CloudCoveragePercent" = @cloudCoveragePercent,
                    "SpatialResolutionMeters" = @spatialResolutionMeters, "AssetReference" = @assetReference,
                    "Notes" = @notes, "Footprint" = CASE WHEN candidate.geom IS NULL THEN NULL ELSE candidate.geom::geography END,
                    "UpdatedAtUtc" = @updatedAtUtc
                FROM eligible CROSS JOIN candidate
                WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")} AND s."Id" = @id AND s."IsActive"
                RETURNING s.*)
            SELECT u."Id", u."FieldId", u."SeasonId", u."Provider", u."ExternalId", u."Platform", u."AcquiredAtUtc",
                   u."CloudCoveragePercent", u."SpatialResolutionMeters", u."AssetReference", u."Notes",
                   CASE WHEN u."Footprint" IS NULL THEN NULL ELSE ST_AsGeoJSON(u."Footprint"::geometry, 8, 0) END,
                   u."IsActive", u."CreatedAtUtc", u."UpdatedAtUtc"
            FROM updated AS u;
            """;
        return QuerySceneSingleAsync(sql, command => ConfigureSceneWrite(command, organizationId, model), cancellationToken);
    }

    public Task<RemoteSensingSceneSnapshot?> DeactivateSceneAsync(Guid organizationId, Guid sceneId, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            WITH updated AS (
                UPDATE remote_sensing_scenes AS s SET "IsActive" = FALSE, "UpdatedAtUtc" = @updatedAtUtc
                WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")} AND s."Id" = @sceneId RETURNING s.*)
            SELECT u."Id", u."FieldId", u."SeasonId", u."Provider", u."ExternalId", u."Platform", u."AcquiredAtUtc",
                   u."CloudCoveragePercent", u."SpatialResolutionMeters", u."AssetReference", u."Notes",
                   CASE WHEN u."Footprint" IS NULL THEN NULL ELSE ST_AsGeoJSON(u."Footprint"::geometry, 8, 0) END,
                   u."IsActive", u."CreatedAtUtc", u."UpdatedAtUtc"
            FROM updated AS u;
            """;
        return QuerySceneSingleAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "sceneId", sceneId); AddParameter(command, "updatedAtUtc", updatedAtUtc);
        }, cancellationToken);
    }

    public async Task<(IReadOnlyList<VegetationIndexObservationSnapshot> Items, int TotalCount)> ListObservationsAsync(
        Guid organizationId, int skip, int take, Guid? sceneId, Guid? fieldId, Guid? seasonId, Guid? managementZoneId,
        string? indexType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var filter = $"""
            FROM vegetation_index_observations AS o
            JOIN remote_sensing_scenes AS s ON s."Id" = o."SceneId" AND s."OrganizationId" = o."OrganizationId"
            WHERE o."OrganizationId" = @organizationId{ObservationScopeSql("o")}
              AND (CAST(@sceneId AS uuid) IS NULL OR o."SceneId" = CAST(@sceneId AS uuid))
              AND (CAST(@fieldId AS uuid) IS NULL OR o."FieldId" = CAST(@fieldId AS uuid))
              AND (CAST(@seasonId AS uuid) IS NULL OR o."SeasonId" = CAST(@seasonId AS uuid))
              AND (CAST(@managementZoneId AS uuid) IS NULL OR o."ManagementZoneId" = CAST(@managementZoneId AS uuid))
              AND (CAST(@indexType AS text) IS NULL OR o."IndexType" = CAST(@indexType AS text))
              AND (CAST(@fromUtc AS timestamptz) IS NULL OR o."ObservedAtUtc" >= CAST(@fromUtc AS timestamptz))
              AND (CAST(@toUtc AS timestamptz) IS NULL OR o."ObservedAtUtc" <= CAST(@toUtc AS timestamptz))
            """;
        var countSql = "SELECT COUNT(*) " + filter + ";";
        var listSql = """
            SELECT o."Id", o."SceneId", o."FieldId", o."SeasonId", o."ManagementZoneId", o."IndexType", o."CustomIndexName",
                   o."Minimum", o."Maximum", o."Mean", o."Median", o."StandardDeviation", o."ValidCoveragePercent", o."SampleCount",
                   o."Source", s."Platform", o."ObservedAtUtc", o."CreatedAtUtc"
            """ + filter + " ORDER BY o.\"ObservedAtUtc\" DESC, o.\"CreatedAtUtc\" DESC LIMIT @take OFFSET @skip;";

        void Configure(DbCommand command)
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "sceneId", sceneId); AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "seasonId", seasonId); AddParameter(command, "managementZoneId", managementZoneId); AddParameter(command, "indexType", indexType);
            AddParameter(command, "fromUtc", fromUtc); AddParameter(command, "toUtc", toUtc);
        }

        var total = await ExecuteCountAsync(countSql, Configure, cancellationToken);
        var items = await QueryObservationsManyAsync(listSql, command => { Configure(command); AddParameter(command, "take", take); AddParameter(command, "skip", skip); }, cancellationToken);
        return (items, total);
    }

    public Task<VegetationIndexObservationSnapshot?> AddObservationAsync(Guid organizationId, VegetationIndexObservationWriteModel model, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            WITH eligible AS (
                SELECT s."Id"
                FROM remote_sensing_scenes AS s
                LEFT JOIN management_zones AS z ON z."Id" = CAST(@managementZoneId AS uuid)
                WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")} AND s."Id" = @sceneId AND s."IsActive"
                  AND s."FieldId" = @fieldId
                  AND s."SeasonId" IS NOT DISTINCT FROM CAST(@seasonId AS uuid)
                  AND (CAST(@managementZoneId AS uuid) IS NULL OR (z."OrganizationId" = @organizationId AND z."FieldId" = s."FieldId" AND z."IsActive"))
            ), inserted AS (
                INSERT INTO vegetation_index_observations
                    ("Id", "OrganizationId", "SceneId", "FieldId", "SeasonId", "ManagementZoneId", "IndexType", "CustomIndexName",
                     "Minimum", "Maximum", "Mean", "Median", "StandardDeviation", "ValidCoveragePercent", "SampleCount", "Source", "ObservedAtUtc", "CreatedAtUtc")
                SELECT @id, @organizationId, @sceneId, @fieldId, CAST(@seasonId AS uuid), CAST(@managementZoneId AS uuid), @indexType, @customIndexName,
                       @minimum, @maximum, @mean, @median, @standardDeviation, @validCoveragePercent, @sampleCount, @source, @observedAtUtc, @createdAtUtc
                FROM eligible RETURNING *)
            SELECT i."Id", i."SceneId", i."FieldId", i."SeasonId", i."ManagementZoneId", i."IndexType", i."CustomIndexName",
                   i."Minimum", i."Maximum", i."Mean", i."Median", i."StandardDeviation", i."ValidCoveragePercent", i."SampleCount",
                   i."Source", s."Platform", i."ObservedAtUtc", i."CreatedAtUtc"
            FROM inserted AS i JOIN remote_sensing_scenes AS s ON s."Id" = i."SceneId";
            """;
        return QueryObservationSingleAsync(sql, command => ConfigureObservationWrite(command, organizationId, model), cancellationToken);
    }

    public Task<IReadOnlyList<VegetationIndexObservationSnapshot>> ListSeriesAsync(
        Guid organizationId, Guid fieldId, Guid? seasonId, Guid? managementZoneId, string? indexType,
        DateTime? fromUtc, DateTime? toUtc, int take, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT o."Id", o."SceneId", o."FieldId", o."SeasonId", o."ManagementZoneId", o."IndexType", o."CustomIndexName",
                   o."Minimum", o."Maximum", o."Mean", o."Median", o."StandardDeviation", o."ValidCoveragePercent", o."SampleCount",
                   o."Source", s."Platform", o."ObservedAtUtc", o."CreatedAtUtc"
            FROM vegetation_index_observations AS o
            JOIN remote_sensing_scenes AS s ON s."Id" = o."SceneId" AND s."OrganizationId" = o."OrganizationId"
            WHERE o."OrganizationId" = @organizationId{ObservationScopeSql("o")} AND o."FieldId" = @fieldId
              AND (CAST(@seasonId AS uuid) IS NULL OR o."SeasonId" = CAST(@seasonId AS uuid))
              AND (CAST(@managementZoneId AS uuid) IS NULL OR o."ManagementZoneId" = CAST(@managementZoneId AS uuid))
              AND (CAST(@indexType AS text) IS NULL OR o."IndexType" = CAST(@indexType AS text))
              AND (CAST(@fromUtc AS timestamptz) IS NULL OR o."ObservedAtUtc" >= CAST(@fromUtc AS timestamptz))
              AND (CAST(@toUtc AS timestamptz) IS NULL OR o."ObservedAtUtc" <= CAST(@toUtc AS timestamptz))
            ORDER BY o."ObservedAtUtc" ASC, o."CreatedAtUtc" ASC
            LIMIT @take;
            """;
        return QueryObservationsManyAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId); AddParameter(command, "seasonId", seasonId);
            AddParameter(command, "managementZoneId", managementZoneId); AddParameter(command, "indexType", indexType); AddParameter(command, "fromUtc", fromUtc);
            AddParameter(command, "toUtc", toUtc); AddParameter(command, "take", take);
        }, cancellationToken);
    }

    public Task<int> CountScenesAsync(Guid organizationId, Guid fieldId, Guid? seasonId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT COUNT(*) FROM remote_sensing_scenes AS s
            WHERE s."OrganizationId" = @organizationId{SceneScopeSql("s")} AND s."FieldId" = @fieldId AND s."IsActive"
              AND (CAST(@seasonId AS uuid) IS NULL OR s."SeasonId" = CAST(@seasonId AS uuid))
              AND (CAST(@fromUtc AS timestamptz) IS NULL OR s."AcquiredAtUtc" >= CAST(@fromUtc AS timestamptz))
              AND (CAST(@toUtc AS timestamptz) IS NULL OR s."AcquiredAtUtc" <= CAST(@toUtc AS timestamptz));
            """;
        return ExecuteCountAsync(sql, command =>
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId); AddParameter(command, "seasonId", seasonId);
            AddParameter(command, "fromUtc", fromUtc); AddParameter(command, "toUtc", toUtc);
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<RemoteSensingSceneSnapshot>> QueryScenesManyAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); var items = new List<RemoteSensingSceneSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadScene(reader)); return items;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<RemoteSensingSceneSnapshot?> QuerySceneSingleAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken) ? ReadScene(reader) : null;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<IReadOnlyList<VegetationIndexObservationSnapshot>> QueryObservationsManyAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); var items = new List<VegetationIndexObservationSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadObservation(reader)); return items;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<VegetationIndexObservationSnapshot?> QueryObservationSingleAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken) ? ReadObservation(reader) : null;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<int> ExecuteCountAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private static RemoteSensingSceneSnapshot ReadScene(DbDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.GetString(3), reader.GetString(4),
        reader.GetString(5), reader.GetDateTime(6), reader.IsDBNull(7) ? null : reader.GetDecimal(7), reader.IsDBNull(8) ? null : reader.GetDecimal(8),
        reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11),
        reader.GetBoolean(12), reader.GetDateTime(13), reader.GetDateTime(14));

    private static VegetationIndexObservationSnapshot ReadObservation(DbDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.IsDBNull(3) ? null : reader.GetGuid(3), reader.IsDBNull(4) ? null : reader.GetGuid(4),
        reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetDecimal(7), reader.GetDecimal(8), reader.GetDecimal(9),
        reader.GetDecimal(10), reader.GetDecimal(11), reader.GetDecimal(12), reader.IsDBNull(13) ? null : reader.GetInt64(13), reader.GetString(14),
        reader.GetString(15), reader.GetDateTime(16), reader.GetDateTime(17));

    private static void ConfigureSceneWrite(DbCommand command, Guid organizationId, RemoteSensingSceneWriteModel model)
    {
        AddParameter(command, "id", model.Id); AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", model.FieldId);
        AddParameter(command, "seasonId", model.SeasonId); AddParameter(command, "provider", model.Provider); AddParameter(command, "externalId", model.ExternalId);
        AddParameter(command, "platform", model.Platform); AddParameter(command, "acquiredAtUtc", model.AcquiredAtUtc); AddParameter(command, "cloudCoveragePercent", model.CloudCoveragePercent);
        AddParameter(command, "spatialResolutionMeters", model.SpatialResolutionMeters); AddParameter(command, "assetReference", model.AssetReference); AddParameter(command, "notes", model.Notes);
        AddParameter(command, "footprint", model.FootprintGeoJson); AddParameter(command, "createdAtUtc", model.CreatedAtUtc); AddParameter(command, "updatedAtUtc", model.UpdatedAtUtc);
    }

    private static void ConfigureObservationWrite(DbCommand command, Guid organizationId, VegetationIndexObservationWriteModel model)
    {
        AddParameter(command, "id", model.Id); AddParameter(command, "organizationId", organizationId); AddParameter(command, "sceneId", model.SceneId);
        AddParameter(command, "fieldId", model.FieldId); AddParameter(command, "seasonId", model.SeasonId); AddParameter(command, "managementZoneId", model.ManagementZoneId);
        AddParameter(command, "indexType", model.IndexType); AddParameter(command, "customIndexName", model.CustomIndexName); AddParameter(command, "minimum", model.Minimum);
        AddParameter(command, "maximum", model.Maximum); AddParameter(command, "mean", model.Mean); AddParameter(command, "median", model.Median);
        AddParameter(command, "standardDeviation", model.StandardDeviation); AddParameter(command, "validCoveragePercent", model.ValidCoveragePercent);
        AddParameter(command, "sampleCount", model.SampleCount); AddParameter(command, "source", model.Source); AddParameter(command, "observedAtUtc", model.ObservedAtUtc);
        AddParameter(command, "createdAtUtc", model.CreatedAtUtc);
    }

    private string FieldScopeSql(string alias)
    {
        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;
        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";
        return $" AND {alias}.\"FarmId\" IN ({FarmIdSqlList()})";
    }

    private string SceneScopeSql(string alias) => FieldReferenceScopeSql($"{alias}.\"FieldId\"");
    private string ObservationScopeSql(string alias) => FieldReferenceScopeSql($"{alias}.\"FieldId\"");

    private string FieldReferenceScopeSql(string fieldIdExpression)
    {
        if (operationalScope is not { IsInitialized: true, IsRestricted: true }) return string.Empty;
        if (operationalScope.FarmIds.Count == 0) return " AND FALSE";
        return $" AND EXISTS (SELECT 1 FROM fields AS scope_field WHERE scope_field.\"OrganizationId\" = @organizationId AND scope_field.\"Id\" = {fieldIdExpression} AND scope_field.\"FarmId\" IN ({FarmIdSqlList()}))";
    }

    private string FarmIdSqlList() => string.Join(", ", operationalScope!.FarmIds.Select(id => $"'{id:D}'::uuid"));

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value ?? DBNull.Value; command.Parameters.Add(parameter);
    }
}
