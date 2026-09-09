using System.Text.Json;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class OfflineSyncIdempotencyPersistenceTests
{
    [Fact]
    public async Task Completed_operation_is_replayed_without_reclaiming_the_same_operation_id()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var repository = new OfflineSyncIdempotencyRepository(dbContext);
        var transaction = new OfflineSyncTransaction(dbContext);
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string requestHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string resultJson = "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"status\":\"Applied\"}";

        var firstClaim = await transaction.ExecuteAsync(async ct =>
        {
            var claimed = await repository.TryClaimAsync(
                organizationId,
                userId,
                operationId,
                farmId,
                "field",
                entityId,
                "update",
                requestHash,
                now,
                now.AddDays(30),
                ct);
            Assert.True(claimed);
            await repository.CompleteAsync(
                organizationId,
                userId,
                operationId,
                requestHash,
                resultJson,
                now.AddSeconds(1),
                ct);
            return claimed;
        });
        Assert.True(firstClaim);

        var secondClaim = await transaction.ExecuteAsync(ct => repository.TryClaimAsync(
            organizationId,
            userId,
            operationId,
            farmId,
            "field",
            entityId,
            "update",
            requestHash,
            now.AddMinutes(1),
            now.AddDays(30),
            ct));
        Assert.False(secondClaim);

        var stored = await repository.GetAsync(organizationId, userId, operationId);
        Assert.NotNull(stored);
        Assert.Equal("completed", stored.Status);
        Assert.Equal(requestHash, stored.RequestHash);
        Assert.False(string.IsNullOrWhiteSpace(stored.ResultJson));

        using var persisted = JsonDocument.Parse(stored.ResultJson!);
        Assert.Equal("00000000-0000-0000-0000-000000000001", persisted.RootElement.GetProperty("operationId").GetString());
        Assert.Equal("Applied", persisted.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Rolled_back_claim_can_be_claimed_again_on_retry()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var repository = new OfflineSyncIdempotencyRepository(dbContext);
        var transaction = new OfflineSyncTransaction(dbContext);
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string requestHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.ExecuteAsync<bool>(async ct =>
        {
            var claimed = await repository.TryClaimAsync(
                organizationId,
                userId,
                operationId,
                farmId,
                "season",
                entityId,
                "create",
                requestHash,
                now,
                now.AddDays(30),
                ct);
            Assert.True(claimed);
            throw new InvalidOperationException("simulate failure before commit");
        }));

        var retryClaimed = await transaction.ExecuteAsync(ct => repository.TryClaimAsync(
            organizationId,
            userId,
            operationId,
            farmId,
            "season",
            entityId,
            "create",
            requestHash,
            now.AddMinutes(1),
            now.AddDays(30),
            ct));

        Assert.True(retryClaimed);
    }
}
