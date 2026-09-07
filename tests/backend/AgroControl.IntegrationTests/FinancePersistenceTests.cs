using AgroControl.Application.Finance;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Finance;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class FinancePersistenceTests
{
    [Fact]
    public async Task Finance_repository_isolates_tenants_and_calculates_accrual_and_cash_totals()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Finance A", $"finance-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Finance B", $"finance-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(organizationA, organizationB);

        var expenseA = FinancialTransaction.Create(
            organizationA.Id, null, null, FinancialEntryType.Expense, "Fertilizante", "Fornecedor A", 100m,
            new DateOnly(2026, 9, 1), null, null, null, null, null, now);
        var revenueA = FinancialTransaction.Create(
            organizationA.Id, null, null, FinancialEntryType.Revenue, "Venda soja", "Cliente A", 250m,
            new DateOnly(2026, 9, 2), null, null, null, null, null, now);
        revenueA.Settle(new DateOnly(2026, 9, 7), now);
        var expenseB = FinancialTransaction.Create(
            organizationB.Id, null, null, FinancialEntryType.Expense, "Despesa B", null, 999m,
            new DateOnly(2026, 9, 1), null, null, null, null, null, now);

        dbContext.FinancialTransactions.AddRange(expenseA, revenueA, expenseB);
        await dbContext.SaveChangesAsync();

        var repository = new FinanceRepository(dbContext);
        var (items, totalCount) = await repository.ListTransactionsAsync(
            organizationA.Id, 0, 20, null, null, null, null, null, null, null, null);
        Assert.Equal(2, totalCount);
        Assert.All(items, x => Assert.Equal(organizationA.Id, x.OrganizationId));

        var totals = await repository.GetTotalsAsync(organizationA.Id, null, null, null, null, null);
        Assert.Equal(250m, totals.AccruedRevenue);
        Assert.Equal(100m, totals.AccruedExpense);
        Assert.Equal(250m, totals.CashRevenue);
        Assert.Equal(0m, totals.CashExpense);
        Assert.Equal(0m, totals.PendingReceivables);
        Assert.Equal(100m, totals.PendingPayables);
    }

    [Fact]
    public async Task Season_summary_calculates_cost_per_hectare_unit_and_break_even()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organization = Organization.Create("Fazenda Econômica", $"finance-season-{Guid.NewGuid():N}", now);
        var farm = Farm.Create(organization.Id, "Fazenda Modelo", 100m, "Rio Verde", "GO", now);
        var field = Field.Create(organization.Id, farm.Id, "Talhão 01", 50m, now);
        var crop = Crop.Create(organization.Id, "Soja", "Teste", now);
        var season = Season.Create(
            organization.Id, field.Id, crop.Id, "Safra 2026/27",
            new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), 65m, now);
        season.Update(
            field.Id, crop.Id, season.Name, season.StartDate, season.EndDate,
            65m, 60m, SeasonStatus.Harvested, now);

        dbContext.Organizations.Add(organization);
        dbContext.Farms.Add(farm);
        dbContext.Fields.Add(field);
        dbContext.Crops.Add(crop);
        dbContext.Seasons.Add(season);
        dbContext.FinancialTransactions.AddRange(
            FinancialTransaction.Create(
                organization.Id, null, null, FinancialEntryType.Expense, "Custo da safra", null, 15000m,
                new DateOnly(2026, 10, 1), null, farm.Id, field.Id, season.Id, null, now),
            FinancialTransaction.Create(
                organization.Id, null, null, FinancialEntryType.Revenue, "Venda da produção", null, 30000m,
                new DateOnly(2027, 3, 1), null, farm.Id, field.Id, season.Id, null, now));
        await dbContext.SaveChangesAsync();

        var service = new FinanceService(
            new FinanceRepository(dbContext),
            new ProductionRepository(dbContext),
            new UnitOfWork(dbContext));

        var result = await service.GetSeasonSummaryAsync(organization.Id, season.Id);

        Assert.True(result.Succeeded);
        var summary = Assert.IsType<SeasonFinancialSummaryDto>(result.Value);
        Assert.Equal(50m, summary.AreaHectares);
        Assert.Equal(60m, summary.ActualYieldPerHectare);
        Assert.Equal(3000m, summary.ProductionUnits);
        Assert.Equal(30000m, summary.Revenue);
        Assert.Equal(15000m, summary.Expense);
        Assert.Equal(15000m, summary.Result);
        Assert.Equal(50m, summary.MarginPercent);
        Assert.Equal(300m, summary.CostPerHectare);
        Assert.Equal(5m, summary.CostPerUnit);
        Assert.Equal(5m, summary.BreakEvenPricePerUnit);
    }
}
