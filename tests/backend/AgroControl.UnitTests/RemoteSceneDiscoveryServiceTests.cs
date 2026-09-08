using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.UnitTests;

public sealed class RemoteSceneDiscoveryServiceTests
{
    private const string Boundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-50,-10],[-49,-10],[-49,-9],[-50,-9],[-50,-10]]]}";

    [Fact]
    public async Task Search_uses_the_canonical_field_boundary()
    {
        var harness = CreateHarness();
        harness.Client.SearchResult = RemoteSceneDiscoveryCallResult.Success(new RemoteSceneDiscoveryPageDto([], null));

        var result = await harness.Service.SearchAsync(harness.OrganizationId,
            new SearchRemoteScenesCommand(harness.FieldId, null, "earth-search",
                DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 20, "sentinel-2-l2a", 20m));

        Assert.Equal(RemoteSceneDiscoveryResultKind.Success, result.Kind);
        Assert.Equal(1, harness.Client.SearchCalls);
        Assert.NotNull(harness.Client.LastSearch);
        Assert.Equal(Boundary, harness.Client.LastSearch!.GeometryGeoJson);
        Assert.Equal("earth-search", harness.Client.LastSearch.Provider);
        Assert.Equal("sentinel-2-l2a", harness.Client.LastSearch.Collection);
    }

    [Fact]
    public async Task Search_does_not_call_provider_when_field_is_outside_scope()
    {
        var harness = CreateHarness(includeScopedField: false);

        var result = await harness.Service.SearchAsync(harness.OrganizationId,
            new SearchRemoteScenesCommand(harness.FieldId, null, "earth-search",
                DateTime.UtcNow.AddDays(-30), DateTime.UtcNow));

        Assert.Equal(RemoteSceneDiscoveryResultKind.NotFound, result.Kind);
        Assert.Equal(0, harness.Client.SearchCalls);
    }

    [Fact]
    public async Task Import_rejects_asset_that_provider_does_not_mark_as_raster_candidate()
    {
        var harness = CreateHarness();
        harness.Client.ItemResult = RemoteSceneDiscoveryItemCallResult.Success(new RemoteSceneDiscoveryItemDto(
            "earth-search", "sentinel-2-l2a", "S2-001", DateTime.UtcNow,
            Boundary, [-50d, -10d, -49d, -9d], 8m, 10m, "sentinel-2", "sentinel-2",
            [new RemoteSceneDiscoveryAssetDto("thumbnail", "https://example.test/preview.jpg", "image/jpeg", ["thumbnail"], false)]));

        var result = await harness.Service.ImportAsync(harness.OrganizationId,
            new ImportRemoteSceneCommand(harness.FieldId, null, "earth-search", "sentinel-2-l2a", "S2-001", "thumbnail"));

        Assert.Equal(RemoteSceneDiscoveryResultKind.Validation, result.Kind);
        Assert.Equal(1, harness.Client.ItemCalls);
        Assert.Equal(0, harness.RemoteRepository.AddSceneCalls);
    }

    [Fact]
    public async Task Import_re_fetches_the_item_and_persists_only_the_selected_provider_asset()
    {
        var harness = CreateHarness();
        harness.Client.ItemResult = RemoteSceneDiscoveryItemCallResult.Success(new RemoteSceneDiscoveryItemDto(
            "earth-search", "sentinel-2-l2a", "S2-001", new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc),
            Boundary, [-50d, -10d, -49d, -9d], 8m, 10m, "sentinel-2", "sentinel-2",
            [
                new RemoteSceneDiscoveryAssetDto("visual", "https://example.test/visual.jpg", "image/jpeg", ["visual"], false),
                new RemoteSceneDiscoveryAssetDto("red", "https://data.example.test/red.tif", "image/tiff; application=geotiff", ["data"], true)
            ]));
        harness.RemoteRepository.ReturnCreatedScene = true;

        var result = await harness.Service.ImportAsync(harness.OrganizationId,
            new ImportRemoteSceneCommand(harness.FieldId, null, "earth-search", "sentinel-2-l2a", "S2-001", "red"));

        Assert.Equal(RemoteSceneDiscoveryResultKind.Success, result.Kind);
        Assert.Equal(1, harness.Client.ItemCalls);
        Assert.Equal(1, harness.RemoteRepository.AddSceneCalls);
        Assert.NotNull(harness.RemoteRepository.LastAddedScene);
        Assert.Equal("earth-search", harness.RemoteRepository.LastAddedScene!.Provider);
        Assert.Equal("S2-001", harness.RemoteRepository.LastAddedScene.ExternalId);
        Assert.Equal("https://data.example.test/red.tif", harness.RemoteRepository.LastAddedScene.AssetReference);
        Assert.Equal(harness.FieldId, harness.RemoteRepository.LastAddedScene.FieldId);
    }

    private static Harness CreateHarness(bool includeScopedField = true)
    {
        var organizationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var productionField = Field.Create(organizationId, farmId, "Talhão STAC", 80m, DateTime.UtcNow);
        var fieldId = productionField.Id;
        var precision = new FakePrecisionRepository
        {
            Field = includeScopedField
                ? new SpatialFieldSnapshot(fieldId, farmId, "Talhão STAC", 80m, true, Boundary, 79.8m)
                : null
        };
        var production = new FakeProductionRepository
        {
            Field = includeScopedField ? productionField : null
        };
        var remoteRepository = new FakeRemoteSensingRepository();
        var remoteSensing = new RemoteSensingService(remoteRepository, production, precision);
        var client = new FakeDiscoveryClient();
        var service = new RemoteSceneDiscoveryService(client, precision, production, remoteSensing);
        return new Harness(organizationId, fieldId, service, client, remoteRepository);
    }

    private sealed record Harness(
        Guid OrganizationId,
        Guid FieldId,
        RemoteSceneDiscoveryService Service,
        FakeDiscoveryClient Client,
        FakeRemoteSensingRepository RemoteRepository);

    private sealed class FakeDiscoveryClient : IRemoteSceneDiscoveryClient
    {
        public int SearchCalls { get; private set; }
        public int ItemCalls { get; private set; }
        public RemoteSceneDiscoverySearchRequest? LastSearch { get; private set; }
        public RemoteSceneDiscoveryCallResult SearchResult { get; set; } =
            RemoteSceneDiscoveryCallResult.Success(new RemoteSceneDiscoveryPageDto([], null));
        public RemoteSceneDiscoveryItemCallResult ItemResult { get; set; } =
            RemoteSceneDiscoveryItemCallResult.NotFound("not configured");

        public IReadOnlyList<RemoteSceneDiscoveryProviderDto> GetProviders() =>
            [new("earth-search", "Earth Search", true, ["sentinel-2-l2a"])];

        public Task<RemoteSceneDiscoveryCallResult> SearchAsync(RemoteSceneDiscoverySearchRequest request,
            CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            LastSearch = request;
            return Task.FromResult(SearchResult);
        }

        public Task<RemoteSceneDiscoveryItemCallResult> GetItemAsync(string provider, string collection,
            string externalId, CancellationToken cancellationToken = default)
        {
            ItemCalls++;
            return Task.FromResult(ItemResult);
        }
    }

    private sealed class FakePrecisionRepository : IPrecisionAgricultureRepository
    {
        public SpatialFieldSnapshot? Field { get; set; }

        public Task<IReadOnlyList<SpatialFieldSnapshot>> ListFieldsAsync(Guid organizationId, Guid? farmId,
            bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpatialFieldSnapshot>>(Field is null ? [] : [Field]);

        public Task<SpatialFieldSnapshot?> GetFieldAsync(Guid organizationId, Guid fieldId,
            CancellationToken cancellationToken = default) => Task.FromResult(Field?.FieldId == fieldId ? Field : null);

        public Task<SpatialFieldSnapshot?> UpsertBoundaryAsync(Guid organizationId, Guid fieldId, string geoJson,
            DateTime updatedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SpatialFieldSnapshot?> ClearBoundaryAsync(Guid organizationId, Guid fieldId, DateTime updatedAtUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ManagementZoneSnapshot>> ListZonesAsync(Guid organizationId, Guid? fieldId,
            string? type, string? classification, bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ManagementZoneSnapshot>>([]);
        public Task<ManagementZoneSnapshot?> GetZoneAsync(Guid organizationId, Guid zoneId,
            CancellationToken cancellationToken = default) => Task.FromResult<ManagementZoneSnapshot?>(null);
        public Task<ZoneGeometryValidationSnapshot?> ValidateZoneGeometryAsync(Guid organizationId, Guid fieldId,
            string geoJson, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagementZoneSnapshot?> AddZoneAsync(Guid organizationId, ManagementZoneWriteModel model,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ManagementZoneSnapshot>?> AddZonesAtomicallyAsync(Guid organizationId,
            IReadOnlyList<ManagementZoneWriteModel> models, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ManagementZoneSnapshot?> UpdateZoneAsync(Guid organizationId, ManagementZoneWriteModel model,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagementZoneSnapshot?> DeactivateZoneAsync(Guid organizationId, Guid zoneId, DateTime updatedAtUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeProductionRepository : IProductionRepository
    {
        public Field? Field { get; set; }
        public Season? Season { get; set; }

        public Task<(IReadOnlyList<Farm> Items, int TotalCount)> ListFarmsAsync(Guid organizationId, int skip, int take,
            string? search, bool includeInactive, IReadOnlyCollection<Guid>? allowedFarmIds = null, Guid? regionId = null,
            string? stateCode = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Farm>, int)>(([], 0));
        public Task<Farm?> GetFarmAsync(Guid organizationId, Guid farmId, bool tracking,
            CancellationToken cancellationToken = default) => Task.FromResult<Farm?>(null);
        public void AddFarm(Farm farm) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Field> Items, int TotalCount)> ListFieldsAsync(Guid organizationId, int skip, int take,
            Guid? farmId, string? search, bool includeInactive, IReadOnlyCollection<Guid>? allowedFarmIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Field>, int)>((Field is null ? [] : [Field], Field is null ? 0 : 1));
        public Task<Field?> GetFieldAsync(Guid organizationId, Guid fieldId, bool tracking,
            CancellationToken cancellationToken = default) => Task.FromResult(Field?.Id == fieldId ? Field : null);
        public Task<decimal> GetAllocatedFieldAreaAsync(Guid organizationId, Guid farmId, Guid? excludingFieldId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(0m);
        public void AddField(Field field) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Crop> Items, int TotalCount)> ListCropsAsync(Guid organizationId, int skip, int take,
            string? search, bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Crop>, int)>(([], 0));
        public Task<Crop?> GetCropAsync(Guid organizationId, Guid cropId, bool tracking,
            CancellationToken cancellationToken = default) => Task.FromResult<Crop?>(null);
        public void AddCrop(Crop crop) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Season> Items, int TotalCount)> ListSeasonsAsync(Guid organizationId, int skip,
            int take, Guid? fieldId, SeasonStatus? status, string? search, bool includeInactive,
            IReadOnlyCollection<Guid>? allowedFarmIds = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Season>, int)>((Season is null ? [] : [Season], Season is null ? 0 : 1));
        public Task<Season?> GetSeasonAsync(Guid organizationId, Guid seasonId, bool tracking,
            CancellationToken cancellationToken = default) => Task.FromResult(Season?.Id == seasonId ? Season : null);
        public void AddSeason(Season season) => throw new NotSupportedException();
    }

    private sealed class FakeRemoteSensingRepository : IRemoteSensingRepository
    {
        public int AddSceneCalls { get; private set; }
        public bool ReturnCreatedScene { get; set; }
        public RemoteSensingSceneWriteModel? LastAddedScene { get; private set; }

        public Task<(IReadOnlyList<RemoteSensingSceneSnapshot> Items, int TotalCount)> ListScenesAsync(Guid organizationId,
            int skip, int take, Guid? fieldId, Guid? seasonId, string? platform, string? provider, DateTime? fromUtc,
            DateTime? toUtc, bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<RemoteSensingSceneSnapshot>, int)>(([], 0));
        public Task<RemoteSensingSceneSnapshot?> GetSceneAsync(Guid organizationId, Guid sceneId,
            CancellationToken cancellationToken = default) => Task.FromResult<RemoteSensingSceneSnapshot?>(null);
        public Task<RemoteSensingSceneSnapshot?> AddSceneAsync(Guid organizationId, RemoteSensingSceneWriteModel model,
            CancellationToken cancellationToken = default)
        {
            AddSceneCalls++;
            LastAddedScene = model;
            if (!ReturnCreatedScene) return Task.FromResult<RemoteSensingSceneSnapshot?>(null);
            return Task.FromResult<RemoteSensingSceneSnapshot?>(new RemoteSensingSceneSnapshot(
                model.Id, model.FieldId, model.SeasonId, model.Provider, model.ExternalId, model.Platform,
                model.AcquiredAtUtc, model.CloudCoveragePercent, model.SpatialResolutionMeters, model.AssetReference,
                model.Notes, model.FootprintGeoJson, true, model.CreatedAtUtc, model.UpdatedAtUtc));
        }
        public Task<RemoteSensingSceneSnapshot?> UpdateSceneAsync(Guid organizationId, RemoteSensingSceneWriteModel model,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RemoteSensingSceneSnapshot?> DeactivateSceneAsync(Guid organizationId, Guid sceneId,
            DateTime updatedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<VegetationIndexObservationSnapshot> Items, int TotalCount)> ListObservationsAsync(
            Guid organizationId, int skip, int take, Guid? sceneId, Guid? fieldId, Guid? seasonId,
            Guid? managementZoneId, string? indexType, DateTime? fromUtc, DateTime? toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<VegetationIndexObservationSnapshot>, int)>(([], 0));
        public Task<VegetationIndexObservationSnapshot?> AddObservationAsync(Guid organizationId,
            VegetationIndexObservationWriteModel model, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<VegetationIndexObservationSnapshot>> ListSeriesAsync(Guid organizationId, Guid fieldId,
            Guid? seasonId, Guid? managementZoneId, string? indexType, DateTime? fromUtc, DateTime? toUtc, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VegetationIndexObservationSnapshot>>([]);
        public Task<int> CountScenesAsync(Guid organizationId, Guid fieldId, Guid? seasonId, DateTime? fromUtc,
            DateTime? toUtc, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
