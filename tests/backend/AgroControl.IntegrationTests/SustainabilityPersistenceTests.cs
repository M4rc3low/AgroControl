using AgroControl.Application.Sustainability;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Domain.Modules.Sustainability;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class SustainabilityPersistenceTests
{
    [Fact]
    public async Task Sustainability_is_tenant_isolated_idempotent_and_preserves_factor_snapshot()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Sustainability A", $"sustainability-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Sustainability B", $"sustainability-b-{Guid.NewGuid():N}", now);
        var farm = Farm.Create(organizationA.Id, "Fazenda A", 100m, "Rio Verde", "GO", now);
        var field = Field.Create(organizationA.Id, farm.Id, "Talhão 01", 50m, now);
        var crop = Crop.Create(organizationA.Id, "Soja", "Teste", now);
        var season = Season.Create(
            organizationA.Id, field.Id, crop.Id, "Safra 2026/27",
            new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), 65m, now);
        season.Update(field.Id, crop.Id, season.Name, season.StartDate, season.EndDate, 65m, 60m, SeasonStatus.Harvested, now);

        dbContext.Organizations.AddRange(organizationA, organizationB);
        dbContext.Farms.Add(farm);
        dbContext.Fields.Add(field);
        dbContext.Crops.Add(crop);
        dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync();

        var repository = new SustainabilityRepository(dbContext);
        var service = new SustainabilityService(repository, new ProductionRepository(dbContext), new UnitOfWork(dbContext));

        var factorResult = await service.CreateFactorAsync(organizationA.Id, new CreateEmissionFactorCommand(
            "Diesel agrícola", EmissionSourceCategory.Fuel, "L", 2.5m,
            "Fator de teste documentado", null, new DateOnly(2026, 1, 1), null));
        Assert.True(factorResult.Succeeded);

        repository.AddFactor(EmissionFactor.Create(
            organizationB.Id, "Diesel B", EmissionSourceCategory.Fuel, "L", 9m,
            "Fator B", null, new DateOnly(2026, 1, 1), null, now));
        await dbContext.SaveChangesAsync();

        var factorsA = await service.ListFactorsAsync(organizationA.Id, 1, 20, null, null, false);
        Assert.Single(factorsA.Items);
        Assert.All(factorsA.Items, x => Assert.NotEqual("Diesel B", x.Name));

        var activityResult = await service.CreateActivityAsync(organizationA.Id, new CreateEmissionActivityCommand(
            factorResult.Value!.Id, 100m, new DateOnly(2026, 10, 1), EmissionActivityOrigin.External,
            SustainabilityDataQuality.Recorded, "Consumo de diesel da safra", farm.Id, field.Id, season.Id,
            "Machinery", "fueling-001", null));
        Assert.True(activityResult.Succeeded);
        Assert.Equal(250m, activityResult.Value!.EmissionsKgCo2e);

        var duplicate = await service.CreateActivityAsync(organizationA.Id, new CreateEmissionActivityCommand(
            factorResult.Value.Id, 100m, new DateOnly(2026, 10, 1), EmissionActivityOrigin.External,
            SustainabilityDataQuality.Recorded, "Duplicado", farm.Id, field.Id, season.Id,
            "Machinery", "fueling-001", null));
        Assert.False(duplicate.Succeeded);
        Assert.Equal(AgroControl.Application.Common.OperationErrorKind.Conflict, duplicate.ErrorKind);

        var updated = await service.UpdateFactorAsync(organizationA.Id, factorResult.Value.Id, new UpdateEmissionFactorCommand(
            "Diesel agrícola", EmissionSourceCategory.Fuel, "L", 3m,
            "Fator de teste atualizado", null, new DateOnly(2026, 1, 1), null));
        Assert.True(updated.Succeeded);

        var summary = await service.GetSeasonSummaryAsync(organizationA.Id, season.Id);
        Assert.True(summary.Succeeded);
        Assert.Equal(250m, summary.Value!.TotalKgCo2e);
        Assert.Equal(0.25m, summary.Value.TotalTCo2e);
        Assert.Equal(0.005m, summary.Value.TCo2ePerHectare);
        Assert.Equal(0.083333m, summary.Value.KgCo2ePerUnitProduced);

        var persistedActivity = await dbContext.EmissionActivities.AsNoTracking().SingleAsync();
        Assert.Equal(2.5m, persistedActivity.FactorKgCo2ePerUnitSnapshot);
        Assert.Equal(250m, persistedActivity.EmissionsKgCo2e);
    }
}
