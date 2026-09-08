using System.Text.Json;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class RemoteSceneDiscoveryScopeTests
{
    [Fact]
    public async Task Restricted_user_can_discover_Farm_A_but_provider_is_never_called_for_Farm_B()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        Guid organizationId;
        Guid farmAId;
        Guid fieldAId;
        Guid fieldBId;

        await using (var seed = new AgroControlDbContext(options))
        {
            await seed.Database.EnsureDeletedAsync();
            await seed.Database.MigrateAsync();

            var now = DateTime.UtcNow;
            var organization = Organization.Create("Grupo STAC", $"stac-scope-{Guid.NewGuid():N}", now);
            var farmA = Farm.Create(organization.Id, "Fazenda A", 200m, "Campinas", "SP", now,
                null, "BR", "SP", null, null, -22.90m, -47.06m, "America/Sao_Paulo");
            var farmB = Farm.Create(organization.Id, "Fazenda B", 300m, "Sorriso", "MT", now,
                null, "BR", "MT", null, null, -12.54m, -55.72m, "America/Cuiaba");
            var fieldA = Field.Create(organization.Id, farmA.Id, "A-01", 50m, now);
            var fieldB = Field.Create(organization.Id, farmB.Id, "B-01", 60m, now);

            seed.Organizations.Add(organization);
            seed.Farms.AddRange(farmA, farmB);
            seed.Fields.AddRange(fieldA, fieldB);
            await seed.SaveChangesAsync();

            var precision = new PrecisionAgricultureRepository(seed);
            var boundaryA = JsonSerializer.Serialize(
                new GeoJsonPolygonDto("Polygon", [[[-47.10, -22.95], [-47.08, -22.95], [-47.08, -22.93], [-47.10, -22.93], [-47.10, -22.95]]]),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var boundaryB = JsonSerializer.Serialize(
                new GeoJsonPolygonDto("Polygon", [[[-55.75, -12.58], [-55.73, -12.58], [-55.73, -12.56], [-55.75, -12.56], [-55.75, -12.58]]]),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(await precision.UpsertBoundaryAsync(organization.Id, fieldA.Id, boundaryA, now));
            Assert.NotNull(await precision.UpsertBoundaryAsync(organization.Id, fieldB.Id, boundaryB, now));

            organizationId = organization.Id;
            farmAId = farmA.Id;
            fieldAId = fieldA.Id;
            fieldBId = fieldB.Id;
        }

        var scope = new OperationalScopeContext();
        scope.Initialize(organizationId, Guid.NewGuid(), new FarmAccessScopeSnapshot(false, [farmAId], []));
        await using var scopedDb = new AgroControlDbContext(options, scope);

        var precisionScoped = new PrecisionAgricultureRepository(scopedDb, scope);
        var productionScoped = new ProductionRepository(scopedDb);
        var remoteRepository = new RemoteSensingRepository(scopedDb, scope);
        var remoteService = new RemoteSensingService(remoteRepository, productionScoped, precisionScoped);
        var provider = new RecordingDiscoveryClient();
        var discovery = new RemoteSceneDiscoveryService(provider, precisionScoped, productionScoped, remoteService);

        var nowUtc = DateTime.UtcNow;
        var allowed = await discovery.SearchAsync(organizationId,
            new SearchRemoteScenesCommand(fieldAId, null, "earth-search", nowUtc.AddDays(-10), nowUtc));
        Assert.Equal(RemoteSceneDiscoveryResultKind.Success, allowed.Kind);
        Assert.Equal(1, provider.SearchCalls);
        Assert.Contains("-47.1", provider.LastSearch!.GeometryGeoJson);

        var blocked = await discovery.SearchAsync(organizationId,
            new SearchRemoteScenesCommand(fieldBId, null, "earth-search", nowUtc.AddDays(-10), nowUtc));
        Assert.Equal(RemoteSceneDiscoveryResultKind.NotFound, blocked.Kind);
        Assert.Equal(1, provider.SearchCalls);
        Assert.Equal(0, provider.ItemCalls);

        var blockedImport = await discovery.ImportAsync(organizationId,
            new ImportRemoteSceneCommand(fieldBId, null, "earth-search", "sentinel-2-l2a", "foreign-item", "red"));
        Assert.Equal(RemoteSceneDiscoveryResultKind.NotFound, blockedImport.Kind);
        Assert.Equal(0, provider.ItemCalls);
    }

    private sealed class RecordingDiscoveryClient : IRemoteSceneDiscoveryClient
    {
        public int SearchCalls { get; private set; }
        public int ItemCalls { get; private set; }
        public RemoteSceneDiscoverySearchRequest? LastSearch { get; private set; }

        public IReadOnlyList<RemoteSceneDiscoveryProviderDto> GetProviders() =>
            [new("earth-search", "Earth Search", true, ["sentinel-2-l2a"])];

        public Task<RemoteSceneDiscoveryCallResult> SearchAsync(RemoteSceneDiscoverySearchRequest request,
            CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            LastSearch = request;
            return Task.FromResult(RemoteSceneDiscoveryCallResult.Success(new RemoteSceneDiscoveryPageDto([], null)));
        }

        public Task<RemoteSceneDiscoveryItemCallResult> GetItemAsync(string provider, string collection,
            string externalId, CancellationToken cancellationToken = default)
        {
            ItemCalls++;
            return Task.FromResult(RemoteSceneDiscoveryItemCallResult.NotFound("not expected"));
        }
    }
}
