using AgroControl.Domain.Modules.Finance;
using AgroControl.Domain.Modules.Organizations;
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
}
