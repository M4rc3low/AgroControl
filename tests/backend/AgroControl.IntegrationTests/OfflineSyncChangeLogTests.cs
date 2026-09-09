using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class OfflineSyncChangeLogTests
{
    [Fact]
    public async Task ChangeLog_TracksFieldMoveForBothOldAndNewFarmScopes()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organization = Organization.Create(
            "Grupo Sync",
            $"sync-{Guid.NewGuid():N}",
            now);
        var farmA = Farm.Create(organization.Id, "Fazenda A", 200m, "Campinas", "SP", now);
        var farmB = Farm.Create(organization.Id, "Fazenda B", 300m, "Ribeirão Preto", "SP", now);
        var field = Field.Create(organization.Id, farmA.Id, "Talhão 1", 40m, now);
        var crop = Crop.Create(organization.Id, "Soja", null, now);
        var season = Season.Create(
            organization.Id,
            field.Id,
            crop.Id,
            "Safra 26/27",
            new DateOnly(2026, 9, 1),
            null,
            null,
            now);

        dbContext.Organizations.Add(organization);
        dbContext.Farms.AddRange(farmA, farmB);
        dbContext.Fields.Add(field);
        dbContext.Crops.Add(crop);
        dbContext.Seasons.Add(season);
        await dbContext.SaveChangesAsync();

        var repository = new OfflineSyncChangeRepository(dbContext);
        var beforeMove = await repository.GetCurrentSequenceAsync(organization.Id);
        Assert.True(beforeMove > 0);

        field.Update(farmB.Id, field.Name, field.AreaHectares, now.AddMinutes(1));
        await dbContext.SaveChangesAsync();

        var oldFarmChanges = await repository.ListAfterAsync(
            organization.Id,
            farmA.Id,
            beforeMove,
            100);
        var newFarmChanges = await repository.ListAfterAsync(
            organization.Id,
            farmB.Id,
            beforeMove,
            100);

        Assert.Contains(oldFarmChanges, change =>
            change.EntityKind == "field" &&
            change.EntityId == field.Id &&
            change.ChangeType == "delete" &&
            change.FarmId == farmA.Id);
        Assert.Contains(oldFarmChanges, change =>
            change.EntityKind == "season" &&
            change.EntityId == season.Id &&
            change.ChangeType == "delete" &&
            change.FarmId == farmA.Id);

        Assert.Contains(newFarmChanges, change =>
            change.EntityKind == "field" &&
            change.EntityId == field.Id &&
            change.ChangeType == "upsert" &&
            change.FarmId == farmB.Id);
        Assert.Contains(newFarmChanges, change =>
            change.EntityKind == "season" &&
            change.EntityId == season.Id &&
            change.ChangeType == "upsert" &&
            change.FarmId == farmB.Id);

        Assert.DoesNotContain(oldFarmChanges, change =>
            change.EntityKind == "field" &&
            change.EntityId == field.Id &&
            change.ChangeType == "upsert" &&
            change.FarmId == farmB.Id);
    }
}
