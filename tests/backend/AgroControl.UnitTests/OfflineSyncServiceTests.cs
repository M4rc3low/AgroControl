using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Application.Sync;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.UnitTests;

public sealed class OfflineSyncServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ForAccessibleActiveFarm_ReturnsProtocolMetadata()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);
        var farm = Farm.Create(
            organizationId,
            "Fazenda A",
            120m,
            "Campinas",
            "SP",
            now);

        var repository = new FakeProductionRepository(farm);
        var service = new OfflineSyncService(repository, new FakeFarmAccessScope(true));

        var result = await service.GetStatusAsync(organizationId, userId, farm.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(OperationErrorKind.None, result.ErrorKind);
        Assert.NotNull(result.Value);
        Assert.Equal(farm.Id, result.Value.FarmId);
        Assert.Equal("Fazenda A", result.Value.FarmName);
        Assert.Equal(1, result.Value.ProtocolVersion);
        Assert.Equal(1, result.Value.LocalSchemaVersion);
        Assert.Equal(["farm", "field", "crop", "season"], result.Value.EntityKinds);
        Assert.Equal(now, result.Value.FarmUpdatedAtUtc);
        Assert.Equal(1, repository.GetFarmCalls);
    }

    [Fact]
    public async Task GetStatusAsync_ForFarmOutsideAccessScope_IsForbiddenBeforeReadingFarm()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var farm = Farm.Create(
            organizationId,
            "Fazenda A",
            120m,
            "Campinas",
            "SP",
            DateTime.UtcNow);

        var repository = new FakeProductionRepository(farm);
        var service = new OfflineSyncService(repository, new FakeFarmAccessScope(false));

        var result = await service.GetStatusAsync(organizationId, userId, farm.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.Forbidden, result.ErrorKind);
        Assert.Null(result.Value);
        Assert.Equal(0, repository.GetFarmCalls);
    }

    [Fact]
    public async Task GetStatusAsync_ForInactiveFarm_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var farm = Farm.Create(
            organizationId,
            "Fazenda A",
            120m,
            "Campinas",
            "SP",
            DateTime.UtcNow);
        farm.Deactivate(DateTime.UtcNow.AddMinutes(1));

        var repository = new FakeProductionRepository(farm);
        var service = new OfflineSyncService(repository, new FakeFarmAccessScope(true));

        var result = await service.GetStatusAsync(organizationId, userId, farm.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
        Assert.Null(result.Value);
        Assert.Equal(1, repository.GetFarmCalls);
    }

    private sealed class FakeFarmAccessScope(bool allowed) : IFarmAccessScope
    {
        public Task<FarmAccessScopeSnapshot> GetEffectiveScopeAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FarmAccessScopeSnapshot(false, [], []));

        public Task<bool> CanAccessFarmAsync(
            Guid organizationId,
            Guid userId,
            Guid farmId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(allowed);
    }

    private sealed class FakeProductionRepository(Farm farm) : IProductionRepository
    {
        public int GetFarmCalls { get; private set; }

        public Task<Farm?> GetFarmAsync(
            Guid organizationId,
            Guid farmId,
            bool tracking,
            CancellationToken cancellationToken = default)
        {
            GetFarmCalls++;
            return Task.FromResult<Farm?>(
                farm.OrganizationId == organizationId && farm.Id == farmId ? farm : null);
        }

        public Task<(IReadOnlyList<Farm> Items, int TotalCount)> ListFarmsAsync(
            Guid organizationId,
            int skip,
            int take,
            string? search,
            bool includeInactive,
            IReadOnlyCollection<Guid>? allowedFarmIds = null,
            Guid? regionId = null,
            string? stateCode = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void AddFarm(Farm farm) => throw new NotSupportedException();

        public Task<(IReadOnlyList<Field> Items, int TotalCount)> ListFieldsAsync(
            Guid organizationId,
            int skip,
            int take,
            Guid? farmId,
            string? search,
            bool includeInactive,
            IReadOnlyCollection<Guid>? allowedFarmIds = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Field?> GetFieldAsync(
            Guid organizationId,
            Guid fieldId,
            bool tracking,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<decimal> GetAllocatedFieldAreaAsync(
            Guid organizationId,
            Guid farmId,
            Guid? excludingFieldId = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void AddField(Field field) => throw new NotSupportedException();

        public Task<(IReadOnlyList<Crop> Items, int TotalCount)> ListCropsAsync(
            Guid organizationId,
            int skip,
            int take,
            string? search,
            bool includeInactive,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Crop?> GetCropAsync(
            Guid organizationId,
            Guid cropId,
            bool tracking,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void AddCrop(Crop crop) => throw new NotSupportedException();

        public Task<(IReadOnlyList<Season> Items, int TotalCount)> ListSeasonsAsync(
            Guid organizationId,
            int skip,
            int take,
            Guid? fieldId,
            SeasonStatus? status,
            string? search,
            bool includeInactive,
            IReadOnlyCollection<Guid>? allowedFarmIds = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Season?> GetSeasonAsync(
            Guid organizationId,
            Guid seasonId,
            bool tracking,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void AddSeason(Season season) => throw new NotSupportedException();
    }
}
