using AgroControl.Application.Intelligence;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class RasterProcessingPersistenceTests
{
    [Fact]
    public async Task Raster_processing_is_tenant_isolated_idempotent_and_persists_observations_atomically()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var orgA = Organization.Create("Raster A", $"raster-a-{Guid.NewGuid():N}", now);
        var orgB = Organization.Create("Raster B", $"raster-b-{Guid.NewGuid():N}", now);
        var farm = Farm.Create(orgA.Id, "Fazenda Raster", 100m, "Sorriso", "MT", now);
        var field = Field.Create(orgA.Id, farm.Id, "Talhão Raster", 40m, now);
        var crop = Crop.Create(orgA.Id, "Soja", "Teste", now);
        var season = Season.Create(orgA.Id, field.Id, crop.Id, "Safra Raster", new DateOnly(2026, 9, 1), null, 60m, now);
        dbContext.Organizations.AddRange(orgA, orgB);
        dbContext.Farms.Add(farm); dbContext.Fields.Add(field); dbContext.Crops.Add(crop); dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync();

        var precisionRepository = new PrecisionAgricultureRepository(dbContext);
        var precisionService = new PrecisionAgricultureService(precisionRepository);
        var fieldBoundary = new GeoJsonPolygonDto("Polygon", [[[-55.75, -12.78], [-55.68, -12.78], [-55.68, -12.70], [-55.75, -12.70], [-55.75, -12.78]]]);
        Assert.True((await precisionService.UpsertBoundaryAsync(orgA.Id, field.Id, fieldBoundary)).Succeeded);

        var zoneGeometry = new GeoJsonPolygonDto("Polygon", [[[-55.74, -12.77], [-55.72, -12.77], [-55.72, -12.75], [-55.74, -12.75], [-55.74, -12.77]]]);
        var zone = await precisionService.CreateZoneAsync(orgA.Id,
            new CreateManagementZoneCommand(field.Id, "Vegetation", "Zona Norte", null, "Alta", null, "NDVI", zoneGeometry));
        Assert.True(zone.Succeeded);

        var remoteRepository = new RemoteSensingRepository(dbContext);
        var remoteService = new RemoteSensingService(remoteRepository, new ProductionRepository(dbContext), precisionRepository);
        var scene = await remoteService.CreateSceneAsync(orgA.Id, new CreateRemoteSensingSceneCommand(
            field.Id, season.Id, "Synthetic", "raster-001", "Satellite", new DateTime(2026, 9, 8, 14, 0, 0, DateTimeKind.Utc),
            0m, 10m, "/data/raster-001.tif", null, fieldBoundary));
        Assert.True(scene.Succeeded);

        var rasterRepository = new RasterProcessingRepository(dbContext);
        var intelligence = new StubRasterIntelligenceClient(zone.Value!.Id);
        var service = new RasterProcessingService(remoteRepository, precisionRepository, rasterRepository, intelligence);

        var command = new ProcessRasterSceneCommand("NDVI", null, null, 1, true, "integration-raster-key");
        var first = await service.ProcessSceneAsync(orgA.Id, scene.Value!.Id, command);
        Assert.True(first.Succeeded);
        Assert.False(first.Value!.Reused);
        Assert.Equal("Succeeded", first.Value.Run.Status);
        Assert.Equal(2, first.Value.Results.Count);
        Assert.Contains(first.Value.Results, item => item.ManagementZoneId is null);
        Assert.Contains(first.Value.Results, item => item.ManagementZoneId == zone.Value.Id);

        var repeated = await service.ProcessSceneAsync(orgA.Id, scene.Value.Id, command);
        Assert.True(repeated.Succeeded);
        Assert.True(repeated.Value!.Reused);
        Assert.Equal(first.Value.Run.Id, repeated.Value.Run.Id);

        Assert.Equal(2, await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM vegetation_index_observations").SingleAsync());
        Assert.Equal(2, await dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM raster_zonal_results").SingleAsync());

        var otherTenantRuns = await rasterRepository.ListRunsAsync(orgB.Id, scene.Value.Id);
        Assert.Empty(otherTenantRuns);
        var otherTenantResults = await service.ListResultsAsync(orgB.Id, 1, 20, field.Id, null, null, null, null, null);
        Assert.Empty(otherTenantResults.Items);
    }

    private sealed class StubRasterIntelligenceClient(Guid zoneId) : IIntelligenceClient
    {
        public Task<IntelligenceCallResult> PredictYieldAsync(SeasonPredictionData data, CancellationToken cancellationToken = default) =>
            Task.FromResult(IntelligenceCallResult.Unavailable("not used"));

        public Task<RasterIntelligenceCallResult> ProcessRasterAsync(RasterProcessingData data, CancellationToken cancellationToken = default)
        {
            Assert.Equal(2, data.Targets.Count);
            var response = new RasterProcessingIntelligenceDto(
                "v1",
                "ok",
                new RasterMetadataDto("EPSG:4326", 100, 100, -9999m, 10m, 10m),
                [
                    new RasterTargetStatisticsDto("field", 0.2m, 0.9m, 0.61m, 0.62m, 0.08m, 96m, 8000),
                    new RasterTargetStatisticsDto($"zone:{zoneId:D}", 0.4m, 0.9m, 0.72m, 0.73m, 0.05m, 98m, 1800)
                ]);
            return Task.FromResult(RasterIntelligenceCallResult.Success(response));
        }
    }
}
