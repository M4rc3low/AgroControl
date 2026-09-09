using System.Text.Json;
using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Application.Sync;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class OfflinePushIntegrationTests
{
    [Fact]
    public async Task Create_then_replay_same_operation_does_not_duplicate_field()
    {
        await using var fixture = await Fixture.CreateAsync();
        if (!fixture.Available) return;

        var fieldId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var request = new OfflinePushRequestDto(fixture.Farm.Id, [new OfflinePushOperationDto(
            operationId,
            "field",
            fieldId,
            "create",
            null,
            JsonSerializer.SerializeToElement(new
            {
                farmId = fixture.Farm.Id,
                name = "Talhão Offline",
                areaHectares = 25m
            }))]);

        var first = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);
        var replay = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);

        Assert.True(first.Succeeded);
        Assert.Equal("Applied", Assert.Single(first.Value!.Results).Status);
        Assert.True(replay.Succeeded);
        var replayed = Assert.Single(replay.Value!.Results);
        Assert.Equal("Applied", replayed.Status);
        Assert.True(replayed.Replayed);
        Assert.Equal(1, await fixture.Db.Fields.IgnoreQueryFilters().CountAsync(field =>
            field.OrganizationId == fixture.Organization.Id && field.Id == fieldId));
    }

    [Fact]
    public async Task Update_with_stale_server_version_returns_conflict_and_current_server_entity()
    {
        await using var fixture = await Fixture.CreateAsync();
        if (!fixture.Available) return;

        var createdAt = DatabaseTimestamp.NormalizeUtc(DateTime.UtcNow.AddMinutes(-5));
        var field = Field.Create(fixture.Organization.Id, fixture.Farm.Id, "Talhão A", 20m, createdAt);
        fixture.Db.Fields.Add(field);
        await fixture.Db.SaveChangesAsync();
        var staleVersion = field.UpdatedAtUtc.ToString("O");

        field.Update(fixture.Farm.Id, "Talhão alterado no servidor", 21m, DatabaseTimestamp.NormalizeUtc(createdAt.AddMinutes(1)));
        await fixture.Db.SaveChangesAsync();

        var request = new OfflinePushRequestDto(fixture.Farm.Id, [new OfflinePushOperationDto(
            Guid.NewGuid(),
            "field",
            field.Id,
            "update",
            staleVersion,
            JsonSerializer.SerializeToElement(new
            {
                farmId = fixture.Farm.Id,
                name = "Alteração do dispositivo",
                areaHectares = 22m
            }))]);

        var result = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);

        Assert.True(result.Succeeded);
        var operation = Assert.Single(result.Value!.Results);
        Assert.Equal("Conflict", operation.Status);
        Assert.Equal("VersionConflict", operation.ErrorCode);
        var current = Assert.IsType<FieldDto>(operation.ServerEntity);
        Assert.Equal("Talhão alterado no servidor", current.Name);
        Assert.Equal(field.UpdatedAtUtc.ToString("O"), operation.ServerVersion);
    }

    [Fact]
    public async Task Batch_can_apply_one_operation_and_reject_another_without_losing_the_applied_ack()
    {
        await using var fixture = await Fixture.CreateAsync();
        if (!fixture.Available) return;

        var validId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();
        var request = new OfflinePushRequestDto(fixture.Farm.Id,
        [
            new OfflinePushOperationDto(
                Guid.NewGuid(), "field", validId, "create", null,
                JsonSerializer.SerializeToElement(new { farmId = fixture.Farm.Id, name = "Válido", areaHectares = 10m })),
            new OfflinePushOperationDto(
                Guid.NewGuid(), "field", invalidId, "create", null,
                JsonSerializer.SerializeToElement(new { farmId = fixture.Farm.Id, name = "Inválido", areaHectares = -1m }))
        ]);

        var result = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Results.Count);
        Assert.Equal("Applied", result.Value.Results[0].Status);
        Assert.Equal("ValidationError", result.Value.Results[1].Status);
        Assert.Equal("DomainValidation", result.Value.Results[1].ErrorCode);
        Assert.Equal(1, await fixture.Db.Fields.IgnoreQueryFilters().CountAsync(field =>
            field.OrganizationId == fixture.Organization.Id && (field.Id == validId || field.Id == invalidId)));
    }

    [Fact]
    public async Task Cross_farm_field_payload_is_rejected_without_creating_entity()
    {
        await using var fixture = await Fixture.CreateAsync();
        if (!fixture.Available) return;

        var otherFarm = Farm.Create(
            fixture.Organization.Id,
            "Fazenda B",
            100m,
            "Ribeirão Preto",
            "SP",
            DatabaseTimestamp.UtcNow());
        fixture.Db.Farms.Add(otherFarm);
        await fixture.Db.SaveChangesAsync();

        var fieldId = Guid.NewGuid();
        var request = new OfflinePushRequestDto(fixture.Farm.Id, [new OfflinePushOperationDto(
            Guid.NewGuid(),
            "field",
            fieldId,
            "create",
            null,
            JsonSerializer.SerializeToElement(new
            {
                farmId = otherFarm.Id,
                name = "Tentativa B",
                areaHectares = 10m
            }))]);

        var result = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);

        Assert.True(result.Succeeded);
        var operation = Assert.Single(result.Value!.Results);
        Assert.Equal("Forbidden", operation.Status);
        Assert.Equal("CrossFarmMutation", operation.ErrorCode);
        Assert.False(await fixture.Db.Fields.IgnoreQueryFilters().AnyAsync(field => field.Id == fieldId));
    }

    [Fact]
    public async Task Access_revoked_mid_batch_keeps_first_ack_and_blocks_later_operation()
    {
        var scope = new CountingFarmScope(allowedCalls: 3);
        await using var fixture = await Fixture.CreateAsync(scope);
        if (!fixture.Available) return;

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var request = new OfflinePushRequestDto(fixture.Farm.Id,
        [
            new OfflinePushOperationDto(
                Guid.NewGuid(), "field", firstId, "create", null,
                JsonSerializer.SerializeToElement(new { farmId = fixture.Farm.Id, name = "Primeiro", areaHectares = 10m })),
            new OfflinePushOperationDto(
                Guid.NewGuid(), "field", secondId, "create", null,
                JsonSerializer.SerializeToElement(new { farmId = fixture.Farm.Id, name = "Segundo", areaHectares = 10m }))
        ]);

        var result = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);

        Assert.True(result.Succeeded);
        Assert.Equal("Applied", result.Value!.Results[0].Status);
        Assert.Equal("Forbidden", result.Value.Results[1].Status);
        Assert.Equal("FarmAccessRevoked", result.Value.Results[1].ErrorCode);
        Assert.True(await fixture.Db.Fields.IgnoreQueryFilters().AnyAsync(field => field.Id == firstId));
        Assert.False(await fixture.Db.Fields.IgnoreQueryFilters().AnyAsync(field => field.Id == secondId));
    }

    [Fact]
    public async Task Push_cannot_cross_organization_boundary_even_when_access_scope_claims_allowance()
    {
        await using var fixture = await Fixture.CreateAsync();
        if (!fixture.Available) return;

        var now = DatabaseTimestamp.UtcNow();
        var otherOrganization = Organization.Create("Outra organização", $"other-{Guid.NewGuid():N}", now);
        var otherFarm = Farm.Create(otherOrganization.Id, "Fazenda Externa", 100m, "Goiânia", "GO", now);
        fixture.Db.Organizations.Add(otherOrganization);
        fixture.Db.Farms.Add(otherFarm);
        await fixture.Db.SaveChangesAsync();

        var fieldId = Guid.NewGuid();
        var request = new OfflinePushRequestDto(otherFarm.Id, [new OfflinePushOperationDto(
            Guid.NewGuid(),
            "field",
            fieldId,
            "create",
            null,
            JsonSerializer.SerializeToElement(new
            {
                farmId = otherFarm.Id,
                name = "Tentativa cross-tenant",
                areaHectares = 10m
            }))]);

        var result = await fixture.Push.PushAsync(fixture.Organization.Id, fixture.UserId, request);

        Assert.False(result.Succeeded);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
        Assert.False(await fixture.Db.Fields.IgnoreQueryFilters().AnyAsync(field => field.Id == fieldId));
    }

    private sealed class AllowFarmScope : IFarmAccessScope
    {
        public Task<FarmAccessScopeSnapshot> GetEffectiveScopeAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FarmAccessScopeSnapshot(true, [], []));

        public Task<bool> CanAccessFarmAsync(
            Guid organizationId,
            Guid userId,
            Guid farmId,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class CountingFarmScope(int allowedCalls) : IFarmAccessScope
    {
        private int calls;

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
            Task.FromResult(Interlocked.Increment(ref calls) <= allowedCalls);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(bool available, AgroControlDbContext db, Organization organization, Farm farm, Guid userId, OfflinePushService push)
        {
            Available = available;
            Db = db;
            Organization = organization;
            Farm = farm;
            UserId = userId;
            Push = push;
        }

        public bool Available { get; }
        public AgroControlDbContext Db { get; }
        public Organization Organization { get; }
        public Farm Farm { get; }
        public Guid UserId { get; }
        public OfflinePushService Push { get; }

        public static async Task<Fixture> CreateAsync(IFarmAccessScope? accessScope = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
            var options = new DbContextOptionsBuilder<AgroControlDbContext>()
                .UseNpgsql(connectionString ?? "Host=localhost;Database=unused")
                .Options;
            var db = new AgroControlDbContext(options);
            var now = DatabaseTimestamp.UtcNow();
            var organization = Organization.Create("Offline Push", $"offline-push-{Guid.NewGuid():N}", now);
            var farm = Farm.Create(organization.Id, "Fazenda A", 200m, "Campinas", "SP", now);
            var userId = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(connectionString))
                return new Fixture(false, db, organization, farm, userId, null!);

            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            db.Organizations.Add(organization);
            db.Farms.Add(farm);
            await db.SaveChangesAsync();

            var repository = new ProductionRepository(db);
            var access = accessScope ?? new AllowFarmScope();
            var unitOfWork = new UnitOfWork(db);
            var fieldService = new FieldService(repository, access, unitOfWork);
            var seasonService = new SeasonService(repository, access, unitOfWork);
            var push = new OfflinePushService(
                access,
                repository,
                fieldService,
                seasonService,
                new OfflineSyncIdempotencyRepository(db),
                new OfflineSyncConcurrencyRepository(db),
                new OfflineSyncTransaction(db));

            return new Fixture(true, db, organization, farm, userId, push);
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
