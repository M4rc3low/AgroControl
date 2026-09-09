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
        var farm = Farm.Create(organizationId, "Fazenda A", 120m, "Campinas", "SP", now);
        var repository = new FakeProductionRepository(farm);
        var service = CreateService(repository);

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
        var farm = Farm.Create(organizationId, "Fazenda A", 120m, "Campinas", "SP", DateTime.UtcNow);
        var repository = new FakeProductionRepository(farm);
        var service = CreateService(repository, allowed: false);

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
        var farm = Farm.Create(organizationId, "Fazenda A", 120m, "Campinas", "SP", DateTime.UtcNow);
        farm.Deactivate(DateTime.UtcNow.AddMinutes(1));
        var repository = new FakeProductionRepository(farm);
        var service = CreateService(repository);

        var result = await service.GetStatusAsync(organizationId, userId, farm.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
        Assert.Null(result.Value);
        Assert.Equal(1, repository.GetFarmCalls);
    }

    [Fact]
    public async Task BootstrapAsync_CapturesWatermarkAndReturnsProtectedCursor()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var farm = Farm.Create(organizationId, "Fazenda A", 120m, "Campinas", "SP", DateTime.UtcNow);
        var repository = new FakeProductionRepository(farm);
        var changes = new FakeChangeRepository { CurrentSequence = 42 };
        var service = CreateService(repository, changeRepository: changes);

        var result = await service.BootstrapAsync(organizationId, userId, farm.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.WatermarkSequence);
        Assert.Equal("cursor:42", result.Value.Cursor);
        Assert.Empty(result.Value.Fields);
        Assert.Empty(result.Value.Crops);
        Assert.Empty(result.Value.Seasons);
    }

    [Fact]
    public async Task PullAsync_FieldMovedToAnotherFarm_ReturnsDeleteWithoutLeakingPayload()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var farmA = Farm.Create(organizationId, "Fazenda A", 120m, "Campinas", "SP", now);
        var farmB = Farm.Create(organizationId, "Fazenda B", 130m, "Cuiabá", "MT", now);
        var movedField = Field.Create(organizationId, farmB.Id, "Talhão movido", 10m, now);
        var repository = new FakeProductionRepository(farmA, fields: [movedField]);
        var change = new OfflineSyncChangeEntry(
            11,
            organizationId,
            farmA.Id,
            "field",
            movedField.Id,
            "upsert",
            now);
        var changes = new FakeChangeRepository([change]);
        var service = CreateService(repository, changeRepository: changes);

        var result = await service.PullAsync(organizationId, userId, farmA.Id, "cursor:10");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        var pulled = Assert.Single(result.Value.Changes);
        Assert.Equal("field", pulled.EntityKind);
        Assert.Equal(movedField.Id, pulled.EntityId);
        Assert.Equal("delete", pulled.ChangeType);
        Assert.Null(pulled.Payload);
        Assert.Equal(11, result.Value.LastSequence);
        Assert.Equal("cursor:11", result.Value.Cursor);
    }

    [Fact]
    public async Task PullAsync_InvalidCursor_DoesNotReadChangeLog()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var farm = Farm.Create(organizationId, "Fazenda A", 120m, "Campinas", "SP", DateTime.UtcNow);
        var repository = new FakeProductionRepository(farm);
        var changes = new FakeChangeRepository();
        var cursorProtector = new FakeCursorProtector { ForceInvalid = true };
        var service = CreateService(repository, changeRepository: changes, cursorProtector: cursorProtector);

        var result = await service.PullAsync(organizationId, userId, farm.Id, "tampered");

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.Validation, result.ErrorKind);
        Assert.Equal(0, changes.ListCalls);
    }

    private static OfflineSyncService CreateService(
        FakeProductionRepository repository,
        bool allowed = true,
        FakeChangeRepository? changeRepository = null,
        FakeCursorProtector? cursorProtector = null) =>
        new(
            repository,
            new FakeFarmAccessScope(allowed),
            changeRepository ?? new FakeChangeRepository(),
            cursorProtector ?? new FakeCursorProtector());

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

    private sealed class FakeChangeRepository : IOfflineSyncChangeRepository
    {
        private readonly IReadOnlyList<OfflineSyncChangeEntry> _entries;

        public FakeChangeRepository(IReadOnlyList<OfflineSyncChangeEntry>? entries = null)
        {
            _entries = entries ?? [];
        }

        public long CurrentSequence { get; init; }
        public int ListCalls { get; private set; }

        public Task<long> GetCurrentSequenceAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) => Task.FromResult(CurrentSequence);

        public Task<IReadOnlyList<OfflineSyncChangeEntry>> ListAfterAsync(
            Guid organizationId,
            Guid farmId,
            long afterSequence,
            int take,
            CancellationToken cancellationToken = default)
        {
            ListCalls++;
            IReadOnlyList<OfflineSyncChangeEntry> result = _entries
                .Where(item => item.OrganizationId == organizationId && item.Sequence > afterSequence)
                .OrderBy(item => item.Sequence)
                .Take(take)
                .ToArray();
            return Task.FromResult(result);
        }
    }

    private sealed class FakeCursorProtector : IOfflineSyncCursorProtector
    {
        public bool ForceInvalid { get; init; }

        public string Protect(Guid organizationId, Guid farmId, long sequence, DateTime issuedAtUtc) =>
            $"cursor:{sequence}";

        public OfflineSyncCursorValidation Validate(
            string cursor,
            Guid expectedOrganizationId,
            Guid expectedFarmId,
            DateTime nowUtc)
        {
            if (ForceInvalid || !cursor.StartsWith("cursor:", StringComparison.Ordinal) ||
                !long.TryParse(cursor[7..], out var sequence))
                return OfflineSyncCursorValidation.Invalid("Sync cursor is invalid.");
            return OfflineSyncCursorValidation.Valid(sequence);
        }
    }

    private sealed class FakeProductionRepository : IProductionRepository
    {
        private readonly Farm _farm;
        private readonly IReadOnlyList<Field> _fields;
        private readonly IReadOnlyList<Crop> _crops;
        private readonly IReadOnlyList<Season> _seasons;

        public FakeProductionRepository(
            Farm farm,
            IReadOnlyList<Field>? fields = null,
            IReadOnlyList<Crop>? crops = null,
            IReadOnlyList<Season>? seasons = null)
        {
            _farm = farm;
            _fields = fields ?? [];
            _crops = crops ?? [];
            _seasons = seasons ?? [];
        }

        public int GetFarmCalls { get; private set; }

        public Task<Farm?> GetFarmAsync(
            Guid organizationId,
            Guid farmId,
            bool tracking,
            CancellationToken cancellationToken = default)
        {
            GetFarmCalls++;
            return Task.FromResult<Farm?>(
                _farm.OrganizationId == organizationId && _farm.Id == farmId ? _farm : null);
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
            CancellationToken cancellationToken = default)
        {
            var items = _fields
                .Where(item => item.OrganizationId == organizationId)
                .Where(item => farmId is null || item.FarmId == farmId)
                .Where(item => allowedFarmIds is null || allowedFarmIds.Contains(item.FarmId))
                .ToArray();
            return Task.FromResult(((IReadOnlyList<Field>)items, items.Length));
        }

        public Task<Field?> GetFieldAsync(
            Guid organizationId,
            Guid fieldId,
            bool tracking,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Field?>(_fields.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == fieldId));

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
            CancellationToken cancellationToken = default)
        {
            var items = _crops.Where(item => item.OrganizationId == organizationId).ToArray();
            return Task.FromResult(((IReadOnlyList<Crop>)items, items.Length));
        }

        public Task<Crop?> GetCropAsync(
            Guid organizationId,
            Guid cropId,
            bool tracking,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Crop?>(_crops.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == cropId));

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
            CancellationToken cancellationToken = default)
        {
            var items = _seasons
                .Where(item => item.OrganizationId == organizationId)
                .Where(item => fieldId is null || item.FieldId == fieldId)
                .Where(item => allowedFarmIds is null || _fields.Any(field => field.Id == item.FieldId && allowedFarmIds.Contains(field.FarmId)))
                .ToArray();
            return Task.FromResult(((IReadOnlyList<Season>)items, items.Length));
        }

        public Task<Season?> GetSeasonAsync(
            Guid organizationId,
            Guid seasonId,
            bool tracking,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Season?>(_seasons.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == seasonId));

        public void AddSeason(Season season) => throw new NotSupportedException();
    }
}
