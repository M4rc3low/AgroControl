using System.Text.Json;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class PrecisionAgriculturePersistenceTests
{
    [Fact]
    public async Task Precision_repository_persists_postgis_boundary_and_isolates_tenants()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Grupo Geo A", $"geo-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Grupo Geo B", $"geo-b-{Guid.NewGuid():N}", now);
        var farmA = Farm.Create(organizationA.Id, "Fazenda Geo A", 100m, "Rio Verde", "GO", now);
        var farmB = Farm.Create(organizationB.Id, "Fazenda Geo B", 100m, "Sorriso", "MT", now);
        var fieldA = Field.Create(organizationA.Id, farmA.Id, "Talhão Geo A", 10m, now);
        var fieldB = Field.Create(organizationB.Id, farmB.Id, "Talhão Geo B", 12m, now);

        dbContext.Organizations.AddRange(organizationA, organizationB);
        dbContext.Farms.AddRange(farmA, farmB);
        dbContext.Fields.AddRange(fieldA, fieldB);
        await dbContext.SaveChangesAsync();

        var repository = new PrecisionAgricultureRepository(dbContext);
        var polygon = new GeoJsonPolygonDto("Polygon", [[[-47.1000, -15.7000], [-47.0900, -15.7000], [-47.0900, -15.6900], [-47.1000, -15.6900], [-47.1000, -15.7000]]]);
        var geoJson = JsonSerializer.Serialize(polygon, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var saved = await repository.UpsertBoundaryAsync(organizationA.Id, fieldA.Id, geoJson, DateTime.UtcNow);
        Assert.NotNull(saved);
        Assert.NotNull(saved.SpatialAreaHectares);
        Assert.True(saved.SpatialAreaHectares > 0m);
        Assert.Contains("Polygon", saved.BoundaryGeoJson);
        Assert.Null(await repository.GetFieldAsync(organizationB.Id, fieldA.Id));

        var visibleToA = await repository.ListFieldsAsync(organizationA.Id, null, false);
        var only = Assert.Single(visibleToA);
        Assert.Equal(fieldA.Id, only.FieldId);
        Assert.NotNull(only.BoundaryGeoJson);
    }
}
