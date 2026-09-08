using System.Text.Json;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class ManagementZonePersistenceTests
{
    [Fact]
    public async Task Management_zones_are_tenant_isolated_contained_and_batch_import_is_atomic()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var orgA = Organization.Create("Geo Zones A", $"zones-a-{Guid.NewGuid():N}", now);
        var orgB = Organization.Create("Geo Zones B", $"zones-b-{Guid.NewGuid():N}", now);
        var farm = Farm.Create(orgA.Id, "Fazenda Zonas", 100m, "Rio Verde", "GO", now);
        var field = Field.Create(orgA.Id, farm.Id, "Talhão Zonas", 20m, now);
        dbContext.Organizations.AddRange(orgA, orgB); dbContext.Farms.Add(farm); dbContext.Fields.Add(field); await dbContext.SaveChangesAsync();

        var repository = new PrecisionAgricultureRepository(dbContext);
        var service = new PrecisionAgricultureService(repository);
        var boundary = new GeoJsonPolygonDto("Polygon", [[[-47.10, -15.70], [-47.08, -15.70], [-47.08, -15.68], [-47.10, -15.68], [-47.10, -15.70]]]);
        Assert.True((await service.UpsertBoundaryAsync(orgA.Id, field.Id, boundary)).Succeeded);

        var inside = new GeoJsonPolygonDto("Polygon", [[[-47.095, -15.695], [-47.090, -15.695], [-47.090, -15.690], [-47.095, -15.690], [-47.095, -15.695]]]);
        var created = await service.CreateZoneAsync(orgA.Id, new CreateManagementZoneCommand(field.Id, "Soil", "pH alto", null, "Alta", 6.8m, "pH", inside));
        Assert.True(created.Succeeded);
        Assert.True(created.Value!.SpatialAreaHectares > 0m);
        Assert.Single(await service.ListZonesAsync(orgA.Id, field.Id, null, null, false));
        Assert.Empty(await service.ListZonesAsync(orgB.Id, field.Id, null, null, false));

        var outside = new GeoJsonPolygonDto("Polygon", [[[-47.20, -15.80], [-47.19, -15.80], [-47.19, -15.79], [-47.20, -15.79], [-47.20, -15.80]]]);
        var rejected = await service.CreateZoneAsync(orgA.Id, new CreateManagementZoneCommand(field.Id, "Custom", "Fora", null, null, null, null, outside));
        Assert.False(rejected.Succeeded);

        var batchJson = $$"""
        {"type":"FeatureCollection","features":[
          {"type":"Feature","properties":{"name":"Dentro 2","zoneType":"Vegetation","classification":"Vigor alto","value":0.78,"unit":"NDVI"},"geometry":{{JsonSerializer.Serialize(inside)}}},
          {"type":"Feature","properties":{"name":"Fora 2","zoneType":"Custom"},"geometry":{{JsonSerializer.Serialize(outside)}}}
        ]}
        """;
        using var document = JsonDocument.Parse(batchJson);
        var import = await service.ImportZonesAsync(orgA.Id, field.Id, document.RootElement.Clone());
        Assert.False(import.Succeeded);
        Assert.Single(await service.ListZonesAsync(orgA.Id, field.Id, null, null, false));

        var export = await service.ExportZonesAsync(orgA.Id, field.Id);
        Assert.True(export.Succeeded);
        Assert.Single(export.Value!.Features);
        Assert.Equal("FeatureCollection", export.Value.Type);
    }
}
