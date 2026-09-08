using System.Data;
using System.Data.Common;
using AgroControl.Application.PrecisionAgriculture;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgroControl.Infrastructure.Persistence;

public sealed class RasterProcessingRepository(AgroControlDbContext dbContext) : IRasterProcessingRepository
{
    private const string RunSelect = """
        SELECT r."Id", r."ProductId", r."SceneId", r."ProcessingKey", r."Status", r."IncludeManagementZones",
               r."RequestedAtUtc", r."StartedAtUtc", r."CompletedAtUtc", r."FailureMessage",
               p."ProductType", p."CustomProductName", p."AssetReference", p."Band", p."Crs",
               p."ResolutionX", p."ResolutionY", p."Nodata", p."Width", p."Height"
        FROM raster_processing_runs AS r
        JOIN raster_products AS p ON p."Id" = r."ProductId" AND p."OrganizationId" = r."OrganizationId"
        """;

    private const string ResultSelect = """
        SELECT z."Id", z."RunId", z."ProductId", z."ObservationId", z."SceneId", z."FieldId", z."SeasonId",
               z."ManagementZoneId", z."IndexType", z."CustomIndexName", z."Minimum", z."Maximum", z."Mean",
               z."Median", z."StandardDeviation", z."ValidCoveragePercent", z."SampleCount", z."Source",
               z."ObservedAtUtc", z."CreatedAtUtc"
        FROM raster_zonal_results AS z
        """;

    public Task<RasterProcessingRunSnapshot?> GetRunByKeyAsync(Guid organizationId, string processingKey,
        CancellationToken cancellationToken = default) => QueryRunSingleAsync(
        RunSelect + " WHERE r.\"OrganizationId\" = @organizationId AND r.\"ProcessingKey\" = @processingKey;",
        command => { AddParameter(command, "organizationId", organizationId); AddParameter(command, "processingKey", processingKey); },
        cancellationToken);

    public async Task<RasterRunCreationResult> CreateRunAsync(Guid organizationId, RasterProductWriteModel product,
        RasterProcessingRunWriteModel run, CancellationToken cancellationToken = default)
    {
        var existing = await GetRunByKeyAsync(organizationId, run.ProcessingKey, cancellationToken);
        if (existing is not null) return new RasterRunCreationResult(existing, false);

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = """
                        INSERT INTO raster_products
                            ("Id", "OrganizationId", "SceneId", "ProductType", "CustomProductName", "AssetReference", "Band", "CreatedAtUtc")
                        SELECT @id, @organizationId, @sceneId, @productType, @customProductName, @assetReference, @band, @createdAtUtc
                        FROM remote_sensing_scenes AS s
                        WHERE s."OrganizationId" = @organizationId AND s."Id" = @sceneId AND s."IsActive";
                        """;
                    AddParameter(command, "id", product.Id); AddParameter(command, "organizationId", organizationId);
                    AddParameter(command, "sceneId", product.SceneId); AddParameter(command, "productType", product.ProductType);
                    AddParameter(command, "customProductName", product.CustomProductName); AddParameter(command, "assetReference", product.AssetReference);
                    AddParameter(command, "band", product.Band); AddParameter(command, "createdAtUtc", product.CreatedAtUtc);
                    if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
                        throw new InvalidOperationException("Scene is not eligible for raster processing.");
                }

                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = """
                        INSERT INTO raster_processing_runs
                            ("Id", "OrganizationId", "ProductId", "SceneId", "ProcessingKey", "Status", "IncludeManagementZones", "RequestedAtUtc")
                        VALUES (@id, @organizationId, @productId, @sceneId, @processingKey, 'Pending', @includeManagementZones, @requestedAtUtc);
                        """;
                    AddParameter(command, "id", run.Id); AddParameter(command, "organizationId", organizationId);
                    AddParameter(command, "productId", run.ProductId); AddParameter(command, "sceneId", run.SceneId);
                    AddParameter(command, "processingKey", run.ProcessingKey); AddParameter(command, "includeManagementZones", run.IncludeManagementZones);
                    AddParameter(command, "requestedAtUtc", run.RequestedAtUtc);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);
                var duplicate = await GetRunByKeyAsync(organizationId, run.ProcessingKey, cancellationToken);
                if (duplicate is null) throw;
                return new RasterRunCreationResult(duplicate, false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }

        var created = await GetRunByKeyAsync(organizationId, run.ProcessingKey, cancellationToken)
            ?? throw new InvalidOperationException("Raster processing run was not persisted.");
        return new RasterRunCreationResult(created, true);
    }

    public async Task<RasterProcessingRunSnapshot?> MarkProcessingAsync(Guid organizationId, Guid runId,
        DateTime startedAtUtc, CancellationToken cancellationToken = default)
    {
        await ExecuteNonQueryAsync("""
            UPDATE raster_processing_runs SET "Status" = 'Processing', "StartedAtUtc" = @startedAtUtc, "FailureMessage" = NULL
            WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" = 'Pending';
            """, command => { AddParameter(command, "organizationId", organizationId); AddParameter(command, "runId", runId); AddParameter(command, "startedAtUtc", startedAtUtc); }, cancellationToken);
        return await GetRunAsync(organizationId, runId, cancellationToken);
    }

    public async Task<RasterProcessingRunSnapshot?> MarkFailedAsync(Guid organizationId, Guid runId,
        string failureMessage, DateTime completedAtUtc, CancellationToken cancellationToken = default)
    {
        await ExecuteNonQueryAsync("""
            UPDATE raster_processing_runs
            SET "Status" = 'Failed', "FailureMessage" = @failureMessage, "CompletedAtUtc" = @completedAtUtc
            WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" IN ('Pending', 'Processing');
            """, command => { AddParameter(command, "organizationId", organizationId); AddParameter(command, "runId", runId); AddParameter(command, "failureMessage", failureMessage); AddParameter(command, "completedAtUtc", completedAtUtc); }, cancellationToken);
        return await GetRunAsync(organizationId, runId, cancellationToken);
    }

    public async Task<RasterProcessingRunSnapshot?> CompleteAsync(Guid organizationId, Guid runId,
        RasterCompletionMetadata metadata, IReadOnlyList<RasterZonalResultWriteModel> results,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                Guid productId;
                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = """
                        SELECT "ProductId" FROM raster_processing_runs
                        WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" = 'Processing'
                        FOR UPDATE;
                        """;
                    AddParameter(command, "organizationId", organizationId); AddParameter(command, "runId", runId);
                    var value = await command.ExecuteScalarAsync(cancellationToken);
                    if (value is not Guid id) { await transaction.RollbackAsync(cancellationToken); return null; }
                    productId = id;
                }

                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = """
                        UPDATE raster_products SET "Crs" = @crs, "ResolutionX" = @resolutionX, "ResolutionY" = @resolutionY,
                            "Nodata" = @nodata, "Width" = @width, "Height" = @height
                        WHERE "OrganizationId" = @organizationId AND "Id" = @productId;
                        """;
                    AddParameter(command, "organizationId", organizationId); AddParameter(command, "productId", productId);
                    AddParameter(command, "crs", metadata.Crs); AddParameter(command, "resolutionX", metadata.ResolutionX);
                    AddParameter(command, "resolutionY", metadata.ResolutionY); AddParameter(command, "nodata", metadata.Nodata);
                    AddParameter(command, "width", metadata.Width); AddParameter(command, "height", metadata.Height);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                foreach (var result in results)
                {
                    if (result.ProductId != productId) { await transaction.RollbackAsync(cancellationToken); return null; }
                    await InsertObservationAsync(connection, transaction, organizationId, result, cancellationToken);
                    await InsertZonalResultAsync(connection, transaction, organizationId, runId, result, cancellationToken);
                }

                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = """
                        UPDATE raster_processing_runs SET "Status" = 'Succeeded', "CompletedAtUtc" = @completedAtUtc, "FailureMessage" = NULL
                        WHERE "OrganizationId" = @organizationId AND "Id" = @runId AND "Status" = 'Processing';
                        """;
                    AddParameter(command, "organizationId", organizationId); AddParameter(command, "runId", runId);
                    AddParameter(command, "completedAtUtc", DateTime.UtcNow);
                    if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) { await transaction.RollbackAsync(cancellationToken); return null; }
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
        return await GetRunAsync(organizationId, runId, cancellationToken);
    }

    public Task<IReadOnlyList<RasterProcessingRunSnapshot>> ListRunsAsync(Guid organizationId, Guid sceneId,
        CancellationToken cancellationToken = default) => QueryRunsManyAsync(
        RunSelect + " WHERE r.\"OrganizationId\" = @organizationId AND r.\"SceneId\" = @sceneId ORDER BY r.\"RequestedAtUtc\" DESC;",
        command => { AddParameter(command, "organizationId", organizationId); AddParameter(command, "sceneId", sceneId); }, cancellationToken);

    public Task<IReadOnlyList<RasterZonalResultSnapshot>> ListResultsByRunAsync(Guid organizationId, Guid runId,
        CancellationToken cancellationToken = default) => QueryResultsManyAsync(
        ResultSelect + " WHERE z.\"OrganizationId\" = @organizationId AND z.\"RunId\" = @runId ORDER BY z.\"ManagementZoneId\" NULLS FIRST, z.\"Id\";",
        command => { AddParameter(command, "organizationId", organizationId); AddParameter(command, "runId", runId); }, cancellationToken);

    public async Task<(IReadOnlyList<RasterZonalResultSnapshot> Items, int TotalCount)> ListResultsAsync(Guid organizationId,
        int skip, int take, Guid? fieldId, Guid? seasonId, Guid? managementZoneId, string? indexType, DateTime? fromUtc,
        DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        const string filter = """
            FROM raster_zonal_results AS z
            WHERE z."OrganizationId" = @organizationId
              AND (CAST(@fieldId AS uuid) IS NULL OR z."FieldId" = CAST(@fieldId AS uuid))
              AND (CAST(@seasonId AS uuid) IS NULL OR z."SeasonId" = CAST(@seasonId AS uuid))
              AND (CAST(@managementZoneId AS uuid) IS NULL OR z."ManagementZoneId" = CAST(@managementZoneId AS uuid))
              AND (CAST(@indexType AS text) IS NULL OR z."IndexType" = CAST(@indexType AS text))
              AND (CAST(@fromUtc AS timestamptz) IS NULL OR z."ObservedAtUtc" >= CAST(@fromUtc AS timestamptz))
              AND (CAST(@toUtc AS timestamptz) IS NULL OR z."ObservedAtUtc" <= CAST(@toUtc AS timestamptz))
            """;
        void Configure(DbCommand command)
        {
            AddParameter(command, "organizationId", organizationId); AddParameter(command, "fieldId", fieldId);
            AddParameter(command, "seasonId", seasonId); AddParameter(command, "managementZoneId", managementZoneId);
            AddParameter(command, "indexType", indexType); AddParameter(command, "fromUtc", fromUtc); AddParameter(command, "toUtc", toUtc);
        }
        var total = await ExecuteCountAsync("SELECT COUNT(*) " + filter + ";", Configure, cancellationToken);
        var items = await QueryResultsManyAsync(ResultSelect + filter + " ORDER BY z.\"ObservedAtUtc\" DESC, z.\"CreatedAtUtc\" DESC LIMIT @take OFFSET @skip;",
            command => { Configure(command); AddParameter(command, "take", take); AddParameter(command, "skip", skip); }, cancellationToken);
        return (items, total);
    }

    private Task<RasterProcessingRunSnapshot?> GetRunAsync(Guid organizationId, Guid runId, CancellationToken cancellationToken) =>
        QueryRunSingleAsync(RunSelect + " WHERE r.\"OrganizationId\" = @organizationId AND r.\"Id\" = @runId;",
            command => { AddParameter(command, "organizationId", organizationId); AddParameter(command, "runId", runId); }, cancellationToken);

    private static async Task InsertObservationAsync(DbConnection connection, DbTransaction transaction, Guid organizationId,
        RasterZonalResultWriteModel result, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO vegetation_index_observations
                ("Id", "OrganizationId", "SceneId", "FieldId", "SeasonId", "ManagementZoneId", "IndexType", "CustomIndexName",
                 "Minimum", "Maximum", "Mean", "Median", "StandardDeviation", "ValidCoveragePercent", "SampleCount", "Source", "ObservedAtUtc", "CreatedAtUtc")
            VALUES (@id, @organizationId, @sceneId, @fieldId, CAST(@seasonId AS uuid), CAST(@managementZoneId AS uuid), @indexType,
                    @customIndexName, @minimum, @maximum, @mean, @median, @standardDeviation, @validCoveragePercent, @sampleCount,
                    @source, @observedAtUtc, @createdAtUtc);
            """;
        ConfigureResultParameters(command, organizationId, result);
        AddParameter(command, "id", result.ObservationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertZonalResultAsync(DbConnection connection, DbTransaction transaction, Guid organizationId,
        Guid runId, RasterZonalResultWriteModel result, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO raster_zonal_results
                ("Id", "OrganizationId", "RunId", "ProductId", "ObservationId", "SceneId", "FieldId", "SeasonId", "ManagementZoneId",
                 "IndexType", "CustomIndexName", "Minimum", "Maximum", "Mean", "Median", "StandardDeviation", "ValidCoveragePercent",
                 "SampleCount", "Source", "ObservedAtUtc", "CreatedAtUtc")
            VALUES (@resultId, @organizationId, @runId, @productId, @observationId, @sceneId, @fieldId, CAST(@seasonId AS uuid),
                    CAST(@managementZoneId AS uuid), @indexType, @customIndexName, @minimum, @maximum, @mean, @median,
                    @standardDeviation, @validCoveragePercent, @sampleCount, @source, @observedAtUtc, @createdAtUtc);
            """;
        ConfigureResultParameters(command, organizationId, result);
        AddParameter(command, "resultId", result.Id); AddParameter(command, "runId", runId);
        AddParameter(command, "productId", result.ProductId); AddParameter(command, "observationId", result.ObservationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ConfigureResultParameters(DbCommand command, Guid organizationId, RasterZonalResultWriteModel result)
    {
        AddParameter(command, "organizationId", organizationId); AddParameter(command, "sceneId", result.SceneId);
        AddParameter(command, "fieldId", result.FieldId); AddParameter(command, "seasonId", result.SeasonId);
        AddParameter(command, "managementZoneId", result.ManagementZoneId); AddParameter(command, "indexType", result.IndexType);
        AddParameter(command, "customIndexName", result.CustomIndexName); AddParameter(command, "minimum", result.Minimum);
        AddParameter(command, "maximum", result.Maximum); AddParameter(command, "mean", result.Mean);
        AddParameter(command, "median", result.Median); AddParameter(command, "standardDeviation", result.StandardDeviation);
        AddParameter(command, "validCoveragePercent", result.ValidCoveragePercent); AddParameter(command, "sampleCount", result.SampleCount);
        AddParameter(command, "source", result.Source); AddParameter(command, "observedAtUtc", result.ObservedAtUtc);
        AddParameter(command, "createdAtUtc", result.CreatedAtUtc);
    }

    private async Task<RasterProcessingRunSnapshot?> QueryRunSingleAsync(string sql, Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        var items = await QueryRunsManyAsync(sql, configure, cancellationToken);
        return items.FirstOrDefault();
    }

    private async Task<IReadOnlyList<RasterProcessingRunSnapshot>> QueryRunsManyAsync(string sql, Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<RasterProcessingRunSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadRun(reader));
            return items;
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private async Task<IReadOnlyList<RasterZonalResultSnapshot>> QueryResultsManyAsync(string sql, Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<RasterZonalResultSnapshot>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(ReadResult(reader));
            return items;
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

    private async Task<int> ExecuteNonQueryAsync(string sql, Action<DbCommand> configure, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection(); var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandText = sql; configure(command);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private static RasterProcessingRunSnapshot ReadRun(DbDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5),
        reader.GetDateTime(6), reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        reader.IsDBNull(9) ? null : reader.GetString(9), reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11),
        reader.GetString(12), reader.GetInt32(13), reader.IsDBNull(14) ? null : reader.GetString(14),
        reader.IsDBNull(15) ? null : reader.GetDecimal(15), reader.IsDBNull(16) ? null : reader.GetDecimal(16),
        reader.IsDBNull(17) ? null : reader.GetDecimal(17), reader.IsDBNull(18) ? null : reader.GetInt32(18),
        reader.IsDBNull(19) ? null : reader.GetInt32(19));

    private static RasterZonalResultSnapshot ReadResult(DbDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5),
        reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.IsDBNull(7) ? null : reader.GetGuid(7), reader.GetString(8),
        reader.IsDBNull(9) ? null : reader.GetString(9), reader.GetDecimal(10), reader.GetDecimal(11), reader.GetDecimal(12),
        reader.GetDecimal(13), reader.GetDecimal(14), reader.GetDecimal(15), reader.GetInt64(16), reader.GetString(17),
        reader.GetDateTime(18), reader.GetDateTime(19));

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
