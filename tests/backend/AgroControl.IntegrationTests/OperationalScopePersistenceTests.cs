using AgroControl.Application.Finance;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Finance;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class OperationalScopePersistenceTests
{
    [Fact]
    public async Task Restricted_scope_filters_EF_spatial_remote_sensing_and_raster_access_inside_one_tenant()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        Guid organizationId;
        Guid farmAId;
        Guid fieldAId;
        Guid fieldBId;
        Guid sceneAId;
        Guid sceneBId;

        await using (var seed = new AgroControlDbContext(options))
        {
            await seed.Database.EnsureDeletedAsync();
            await seed.Database.MigrateAsync();

            var now = DateTime.UtcNow;
            var organization = Organization.Create("Grupo Escopo", $"scope-{Guid.NewGuid():N}", now);
            var farmA = Farm.Create(organization.Id, "Fazenda A", 200m, "Rio Verde", "GO", now, null, "BR", "GO", null, null, -17.79m, -50.92m, "America/Sao_Paulo");
            var farmB = Farm.Create(organization.Id, "Fazenda B", 300m, "Sorriso", "MT", now, null, "BR", "MT", null, null, -12.54m, -55.72m, "America/Cuiaba");
            var fieldA = Field.Create(organization.Id, farmA.Id, "A-01", 50m, now);
            var fieldB = Field.Create(organization.Id, farmB.Id, "B-01", 60m, now);
            var crop = Crop.Create(organization.Id, "Soja", "Teste", now);
            var seasonA = Season.Create(organization.Id, fieldA.Id, crop.Id, "Safra A", new DateOnly(2026, 9, 1), null, 60m, now);
            var seasonB = Season.Create(organization.Id, fieldB.Id, crop.Id, "Safra B", new DateOnly(2026, 9, 1), null, 62m, now);

            seed.Organizations.Add(organization);
            seed.Farms.AddRange(farmA, farmB);
            seed.Fields.AddRange(fieldA, fieldB);
            seed.Crops.Add(crop);
            seed.Seasons.AddRange(seasonA, seasonB);
            seed.FinancialTransactions.AddRange(
                FinancialTransaction.Create(organization.Id, null, null, FinancialEntryType.Expense, "Custo A", null, 100m,
                    new DateOnly(2026, 9, 8), null, farmA.Id, fieldA.Id, seasonA.Id, null, now),
                FinancialTransaction.Create(organization.Id, null, null, FinancialEntryType.Expense, "Custo B", null, 900m,
                    new DateOnly(2026, 9, 8), null, farmB.Id, fieldB.Id, seasonB.Id, null, now));
            await seed.SaveChangesAsync();

            var production = new ProductionRepository(seed);
            var precision = new PrecisionAgricultureRepository(seed);
            var remoteRepo = new RemoteSensingRepository(seed);
            var remote = new RemoteSensingService(remoteRepo, production, precision);
            var acquired = new DateTime(2026, 9, 8, 15, 0, 0, DateTimeKind.Utc);
            var sceneA = await remote.CreateSceneAsync(organization.Id,
                new CreateRemoteSensingSceneCommand(fieldA.Id, seasonA.Id, "Test", "scene-a", "Satellite", acquired, 2m, 10m, "file:///scene-a.tif", null, null));
            var sceneB = await remote.CreateSceneAsync(organization.Id,
                new CreateRemoteSensingSceneCommand(fieldB.Id, seasonB.Id, "Test", "scene-b", "Satellite", acquired, 3m, 10m, "file:///scene-b.tif", null, null));
            Assert.True(sceneA.Succeeded);
            Assert.True(sceneB.Succeeded);

            organizationId = organization.Id;
            farmAId = farmA.Id;
            fieldAId = fieldA.Id;
            fieldBId = fieldB.Id;
            sceneAId = sceneA.Value!.Id;
            sceneBId = sceneB.Value!.Id;
        }

        var scope = new OperationalScopeContext();
        scope.Initialize(organizationId, Guid.NewGuid(), new FarmAccessScopeSnapshot(false, [farmAId], []));

        await using var scopedDb = new AgroControlDbContext(options, scope);

        var finance = new FinanceRepository(scopedDb);
        var (transactions, total) = await finance.ListTransactionsAsync(
            organizationId, 0, 20, null, null, null, null, null, null, null, null);
        Assert.Equal(1, total);
        Assert.Single(transactions);
        Assert.Equal(farmAId, transactions[0].FarmId);

        var precisionScoped = new PrecisionAgricultureRepository(scopedDb, scope);
        var spatialFields = await precisionScoped.ListFieldsAsync(organizationId, null, false);
        Assert.Single(spatialFields);
        Assert.Equal(fieldAId, spatialFields[0].FieldId);
        Assert.Null(await precisionScoped.GetFieldAsync(organizationId, fieldBId));

        var remoteScoped = new RemoteSensingRepository(scopedDb, scope);
        var (scenes, sceneCount) = await remoteScoped.ListScenesAsync(
            organizationId, 0, 20, null, null, null, null, null, null, false);
        Assert.Equal(1, sceneCount);
        Assert.Single(scenes);
        Assert.Equal(sceneAId, scenes[0].Id);
        Assert.Null(await remoteScoped.GetSceneAsync(organizationId, sceneBId));

        var raster = new RasterProcessingRepository(scopedDb, scope);
        var nowUtc = DateTime.UtcNow;
        var allowedProduct = new RasterProductWriteModel(Guid.NewGuid(), sceneAId, "NDVI", null, "file:///scene-a.tif", 1, nowUtc);
        var allowedRun = new RasterProcessingRunWriteModel(Guid.NewGuid(), allowedProduct.Id, sceneAId, $"allowed-{Guid.NewGuid():N}", false, nowUtc);
        var allowed = await raster.CreateRunAsync(organizationId, allowedProduct, allowedRun);
        Assert.True(allowed.Created);

        var blockedProduct = new RasterProductWriteModel(Guid.NewGuid(), sceneBId, "NDVI", null, "file:///scene-b.tif", 1, nowUtc);
        var blockedRun = new RasterProcessingRunWriteModel(Guid.NewGuid(), blockedProduct.Id, sceneBId, $"blocked-{Guid.NewGuid():N}", false, nowUtc);
        await Assert.ThrowsAsync<InvalidOperationException>(() => raster.CreateRunAsync(organizationId, blockedProduct, blockedRun));
    }
}
