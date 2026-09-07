using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class IntelligenceDataSourceTests
{
    [Fact]
    public async Task Prediction_data_is_tenant_isolated_and_includes_only_same_crop_history()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Intelligence A", $"int-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Intelligence B", $"int-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(organizationA, organizationB);

        var farmA = Farm.Create(organizationA.Id, "Fazenda A", 500m, null, null, now);
        var farmB = Farm.Create(organizationB.Id, "Fazenda B", 500m, null, null, now);
        dbContext.Farms.AddRange(farmA, farmB);

        var fieldA = Field.Create(organizationA.Id, farmA.Id, "Talhão A", 120m, now);
        var fieldB = Field.Create(organizationB.Id, farmB.Id, "Talhão B", 150m, now);
        dbContext.Fields.AddRange(fieldA, fieldB);

        var soyA = Crop.Create(organizationA.Id, "Soja", "A", now);
        var cornA = Crop.Create(organizationA.Id, "Milho", null, now);
        var soyB = Crop.Create(organizationB.Id, "Soja", "B", now);
        dbContext.Crops.AddRange(soyA, cornA, soyB);

        var current = Season.Create(
            organizationA.Id,
            fieldA.Id,
            soyA.Id,
            "Safra atual",
            new DateOnly(2026, 9, 1),
            null,
            62m,
            now);

        var historicalSoy = Season.Create(
            organizationA.Id,
            fieldA.Id,
            soyA.Id,
            "Safra soja anterior",
            new DateOnly(2025, 9, 1),
            new DateOnly(2026, 2, 1),
            60m,
            now);
        historicalSoy.Update(
            fieldA.Id,
            soyA.Id,
            historicalSoy.Name,
            historicalSoy.StartDate,
            historicalSoy.EndDate,
            60m,
            58m,
            SeasonStatus.Harvested,
            now);

        var historicalCorn = Season.Create(
            organizationA.Id,
            fieldA.Id,
            cornA.Id,
            "Safra milho anterior",
            new DateOnly(2025, 2, 1),
            new DateOnly(2025, 7, 1),
            100m,
            now);
        historicalCorn.Update(
            fieldA.Id,
            cornA.Id,
            historicalCorn.Name,
            historicalCorn.StartDate,
            historicalCorn.EndDate,
            100m,
            98m,
            SeasonStatus.Harvested,
            now);

        var otherTenantSeason = Season.Create(
            organizationB.Id,
            fieldB.Id,
            soyB.Id,
            "Safra B",
            new DateOnly(2025, 9, 1),
            new DateOnly(2026, 2, 1),
            63m,
            now);
        otherTenantSeason.Update(
            fieldB.Id,
            soyB.Id,
            otherTenantSeason.Name,
            otherTenantSeason.StartDate,
            otherTenantSeason.EndDate,
            63m,
            61m,
            SeasonStatus.Harvested,
            now);

        dbContext.Seasons.AddRange(current, historicalSoy, historicalCorn, otherTenantSeason);
        await dbContext.SaveChangesAsync();

        var source = new IntelligenceDataSource(dbContext);
        var data = await source.GetSeasonPredictionDataAsync(organizationA.Id, current.Id);
        var hidden = await source.GetSeasonPredictionDataAsync(organizationB.Id, current.Id);

        Assert.NotNull(data);
        Assert.Equal("Soja", data.CropName);
        Assert.Equal(120m, data.AreaHectares);
        var sample = Assert.Single(data.HistoricalSamples);
        Assert.Equal(58m, sample.ActualYieldPerHectare);
        Assert.Null(hidden);
    }
}
