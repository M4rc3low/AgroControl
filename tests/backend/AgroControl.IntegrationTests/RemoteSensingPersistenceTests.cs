using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class RemoteSensingPersistenceTests
{
    [Fact]
    public async Task Scenes_and_index_observations_are_isolated_idempotent_and_queryable_as_series()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var orgA = Organization.Create("Remote A", $"remote-a-{Guid.NewGuid():N}", now);
        var orgB = Organization.Create("Remote B", $"remote-b-{Guid.NewGuid():N}", now);
        var farm = Farm.Create(orgA.Id, "Fazenda Satélite", 200m, "Sorriso", "MT", now);
        var field = Field.Create(orgA.Id, farm.Id, "Talhão 12", 50m, now);
        var crop = Crop.Create(orgA.Id, "Soja", "M8349", now);
        var season = Season.Create(orgA.Id, field.Id, crop.Id, "Soja 26/27", new DateOnly(2026, 9, 1), null, 65m, now);
        dbContext.Organizations.AddRange(orgA, orgB);
        dbContext.Farms.Add(farm); dbContext.Fields.Add(field); dbContext.Crops.Add(crop); dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync();

        var productionRepository = new ProductionRepository(dbContext);
        var precisionRepository = new PrecisionAgricultureRepository(dbContext);
        var remoteRepository = new RemoteSensingRepository(dbContext);
        var precisionService = new PrecisionAgricultureService(precisionRepository);
        var service = new RemoteSensingService(remoteRepository, productionRepository, precisionRepository);

        var zoneGeometry = new GeoJsonPolygonDto("Polygon", [[[-55.72, -12.75], [-55.71, -12.75], [-55.71, -12.74], [-55.72, -12.74], [-55.72, -12.75]]]);
        var zone = await precisionService.CreateZoneAsync(orgA.Id,
            new CreateManagementZoneCommand(field.Id, "Vegetation", "Vigor Norte", null, "Alto", null, "NDVI", zoneGeometry));
        Assert.True(zone.Succeeded);

        var footprint = new GeoJsonPolygonDto("Polygon", [[[-55.73, -12.76], [-55.70, -12.76], [-55.70, -12.73], [-55.73, -12.73], [-55.73, -12.76]]]);
        var acquiredAt = new DateTime(2026, 9, 8, 13, 0, 0, DateTimeKind.Utc);
        var create = new CreateRemoteSensingSceneCommand(field.Id, season.Id, "Sentinel-2", "S2A-20260908-001", "Satellite",
            acquiredAt, 8.4m, 10m, "s3://agrocontrol/scenes/S2A-20260908-001.tif", "Cena processada externamente", footprint);

        var scene = await service.CreateSceneAsync(orgA.Id, create);
        Assert.True(scene.Succeeded);
        Assert.Equal("Sentinel-2", scene.Value!.Provider);
        Assert.NotNull(scene.Value.Footprint);

        var duplicate = await service.CreateSceneAsync(orgA.Id, create);
        Assert.False(duplicate.Succeeded);
        Assert.Equal(AgroControl.Application.Common.OperationErrorKind.Conflict, duplicate.ErrorKind);

        var orgAList = await service.ListScenesAsync(orgA.Id, 1, 20, field.Id, season.Id, null, null, null, null, false);
        var orgBList = await service.ListScenesAsync(orgB.Id, 1, 20, null, null, null, null, null, null, false);
        Assert.Single(orgAList.Items);
        Assert.Empty(orgBList.Items);

        var observation = await service.CreateObservationAsync(orgA.Id, new CreateVegetationIndexObservationCommand(
            scene.Value.Id, zone.Value!.Id, "NDVI", null, 0.31m, 0.88m, 0.67m, 0.69m, 0.09m, 94.5m, 12000));
        Assert.True(observation.Succeeded);
        Assert.Equal(zone.Value.Id, observation.Value!.ManagementZoneId);
        Assert.Equal(acquiredAt, observation.Value.ObservedAtUtc);

        var invalid = await service.CreateObservationAsync(orgA.Id, new CreateVegetationIndexObservationCommand(
            scene.Value.Id, null, "NDVI", null, -0.2m, 1.4m, 0.7m, 0.6m, 0.1m, 90m, 100));
        Assert.False(invalid.Succeeded);

        var series = await service.GetSeriesAsync(orgA.Id, field.Id, season.Id, zone.Value.Id, "NDVI", null, null);
        Assert.True(series.Succeeded);
        Assert.Single(series.Value!);
        Assert.Equal(0.67m, series.Value![0].Mean);

        var summary = await service.GetSummaryAsync(orgA.Id, field.Id, season.Id, zone.Value.Id, null, null);
        Assert.True(summary.Succeeded);
        Assert.Equal(1, summary.Value!.SceneCount);
        Assert.Single(summary.Value.LatestMetrics);
        Assert.Equal("NDVI", summary.Value.LatestMetrics[0].IndexType);

        var otherTenantObservations = await service.ListObservationsAsync(orgB.Id, 1, 20, null, null, null, null, null, null, null);
        Assert.Empty(otherTenantObservations.Items);

        Assert.True((await service.DeactivateSceneAsync(orgA.Id, scene.Value.Id)).Succeeded);
        var afterDeactivate = await service.CreateObservationAsync(orgA.Id, new CreateVegetationIndexObservationCommand(
            scene.Value.Id, null, "NDVI", null, 0.1m, 0.8m, 0.5m, 0.5m, 0.1m, 90m, 100));
        Assert.False(afterDeactivate.Succeeded);
    }
}
