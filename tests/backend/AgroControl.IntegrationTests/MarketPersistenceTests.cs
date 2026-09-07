using AgroControl.Domain.Modules.Market;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class MarketPersistenceTests
{
    [Fact]
    public async Task Market_repository_isolates_tenants_and_preserves_quote_history()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Market A", $"market-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Market B", $"market-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(organizationA, organizationB);

        var soyA = Commodity.Create(organizationA.Id, "Soja", "SOJA", "BRL", "sc", now);
        var soyB = Commodity.Create(organizationB.Id, "Soja", "SOJA", "BRL", "sc", now);
        dbContext.Commodities.AddRange(soyA, soyB);
        dbContext.MarketQuotes.AddRange(
            MarketQuote.Create(organizationA.Id, soyA.Id, 128m, "BRL", "sc", "Manual", now.AddDays(-1), now),
            MarketQuote.Create(organizationA.Id, soyA.Id, 132m, "BRL", "sc", "Manual", now, now),
            MarketQuote.Create(organizationB.Id, soyB.Id, 999m, "BRL", "sc", "Manual", now, now));
        dbContext.PriceAlerts.Add(PriceAlert.Create(organizationA.Id, soyA.Id, 130m, PriceAlertDirection.AboveOrEqual, now));
        await dbContext.SaveChangesAsync();

        var repository = new MarketRepository(dbContext);
        var (commoditiesA, totalA) = await repository.ListCommoditiesAsync(organizationA.Id, 0, 20, null, false);
        var latestA = await repository.GetLatestQuotesAsync(organizationA.Id, soyA.Id, 2);
        var (alertsA, alertCount) = await repository.ListAlertsAsync(organizationA.Id, 0, 20, soyA.Id, false);

        Assert.Single(commoditiesA);
        Assert.Equal(1, totalA);
        Assert.Equal(2, latestA.Count);
        Assert.Equal(132m, latestA[0].Price);
        Assert.Equal(128m, latestA[1].Price);
        Assert.Single(alertsA);
        Assert.Equal(1, alertCount);
        Assert.True(alertsA[0].IsTriggered(latestA[0].Price));
    }
}
