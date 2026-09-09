using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Application.Sync;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;

namespace AgroControl.UnitTests;

public sealed class OfflineSyncBootstrapTests
{
    [Fact]
    public async Task BootstrapAsync_ReturnsOnlyDataRequiredByTheSelectedFarm()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 9, 1, 0, 0, DateTimeKind.Utc);
        var farm = Farm.Create(organizationId, "Fazenda A", 250m, "Campinas", "SP", now);
        var field = Field.Create(organizationId, farm.Id, "Talhão 1", 50m, now);
        var referencedCrop = Crop.Create(organizationId, "Soja", "Cultivar A", now);
        var unrelatedCrop = Crop.Create(organizationId, "Milho", "Cultivar B", now);
        var season = Season.Create(
            organizationId,
            field.Id,
            referencedCrop.Id,
            "Safra 26/27",
            new DateOnly(2026, 9, 1),
            null,
            4.2m,
            now);

        var repository = new FakeProductionRepository(
            farm,
            [field],
            [referencedCrop, unrelatedCrop],
            [season]);
        var service = new OfflineSyncService(repository, new FakeFarmAccessScope(true));

        var result = await service.BootstrapAsync(organizationId, userId, farm.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(farm.Id, result.Value.Farm.Id);
        Assert.Single(result.Value.Fields);
        Assert.Equal(field.Id, result.Value.Fields[0].Id);
        Assert.Single(result.Value.Seasons);
        Assert.Equal(season.Id, result.Value.Seasons[0].Id);
        Assert.Single(result.Value.Crops);
        Assert.Equal(referencedCrop.Id, result.Value.Crops[0].Id);
        Assert.DoesNotContain(result.Value.Crops, crop => crop.Id == unrelatedCrop.Id);
        Assert.Equal(1, result.Value.ProtocolVersion);
        Assert.Equal(1, result.Value.LocalSchemaVersion);
        Assert.Equal([farm.Id], repository.FieldAllowedFarmIds);
        Assert.Equal([farm.Id], repository.SeasonAllowedFarmIds);
    }

    [Fact]
    public async Task BootstrapAsync_ForFarmOutsideScope_StopsBeforeReadingFarmData()
    {
        var organizationId = Guid.NewGuid();
        var farm = Farm.Create(
            organizationId,
            "Fazenda A",
            250m,
            "Campinas",
            "SP",
            DateTime.UtcNow);
        var repository = new FakeProductionRepository(farm, [], [], []);
        var service = new OfflineSyncService(repository, new FakeFarmAccessScope(false));

        var result = await service.BootstrapAsync(organizationId, Guid.NewGuid(), farm.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.Forbidden, result.ErrorKind);
        Assert.Equal(0, repository.GetFarmCalls);
        Assert.Equal(0, repository.ListFieldsCalls);
        Assert.Equal(0, repository.ListSeasonsCalls);
    }

    [Fact]
    public async Task BootstrapAsync_WhenReferencedCropIsMissing_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var farm = Farm.Create(organizationId, "Fazenda A", 250m, "Campinas", "SP", now);
        var field = Field.Create(organizationId, farm.Id, "Talhão 1", 50m, now);
        var missingCropId = Guid.NewGuid();
        var season = Season.Create(
            organizationId,
            field.Id,
            missingCropId,
            "Safra 26/27",
            new DateOnly(2026, 9, 1),
            null,
            null,
            now);
        var repository = new FakeProductionRepository(farm, [field], [], [season]);
        var service = new OfflineSyncService(repository, new FakeFarmAccessScope(true));

        var result = await service.BootstrapAsync(organizationId, Guid.NewGuid(), farm.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
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

    private sealed class FakeProductionRepository(
        Farm farm,
        IReadOnlyList<Field> fields,
        IReadOnlyList<Crop> crops,
        IReadOnlyList<Season> seasons) : IProductionRepository
    {
        public int GetFarmCalls { get; private set; }
        public int ListFieldsCalls { get; private set; }
        public int ListSeasonsCalls { get; private set; }
        public IReadOnlyCollection<Guid>? FieldAllowedFarmIds { get; private set; }
        public IReadOnlyCollection<Guid>? SeasonAllowedFarmIds { get; private set; }

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

        public Task<(IReadOnlyList<Field> Items, int TotalCount)> ListFieldsAsync(
            Guid organizationId,
            int skip,
            int take,
            Guid? farmId,
            string? search,
            bool includeInactive,
            IReadOnlyCollection<Guid>? allowedFarmIds = null,
            CancellationToken cancellationToken = default)
        {
            ListFieldsCalls++;
            FieldAllowedFarmIds = allowedFarmIds;
            var query = fields
                .Where(item => item.OrganizationId == organizationId)
                .Where(item => farmId is null || item.FarmId == farmId)
                .Where(item => allowedFarmIds is null || allowedFarmIds.Contains(item.FarmId))
                .Where(item => includeInactive || item.IsActive)
                .ToArray();
            return Task.FromResult<(IReadOnlyList<Field>, int)>((query.Skip(skip).Take(take).ToArray(), query.Length));
        }

        public Task<(IReadOnlyList<Season> Items, int TotalCount)> ListSeasonsAsync(
            Guid organizationId,
            int skip,
            int take,
            Guid? fieldId,
            SeasonStatus? status,
            string? search,
            bool includeInactive,
            IReadOnlyCollection<Guid>? allowedFarmIds = null,
            CancellationToken cancellationToken = default)
        {
            ListSeasonsCalls++;
            SeasonAllowedFarmIds = allowedFarmIds;
            var fieldIds = fields
                .Where(field => allowedFarmIds is null || allowedFarmIds.Contains(field.FarmId))
                .Select(field => field.Id)
                .ToHashSet();
            var query = seasons
                .Where(item => item.OrganizationId == organizationId)
                .Where(item => fieldId is null || item.FieldId == fieldId)
                .Where(item => allowedFarmIds is null || fieldIds.Contains(item.FieldId))
                .Where(item => status is null || item.Status == status)
                .Where(item => includeInactive || item.IsActive)
                .ToArray();
            return Task.FromResult<(IReadOnlyList<Season>, int)>((query.Skip(skip).Take(take).ToArray(), query.Length));
        }

        public Task<Crop?> GetCropAsync(
            Guid organizationId,
            Guid cropId,
            bool tracking,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Crop?>(crops.SingleOrDefault(item =>
                item.OrganizationId == organizationId && item.Id == cropId));

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

        public void AddCrop(Crop crop) => throw new NotSupportedException();

        public Task<Season?> GetSeasonAsync(
            Guid organizationId,
            Guid seasonId,
            bool tracking,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void AddSeason(Season season) => throw new NotSupportedException();
    }
}
