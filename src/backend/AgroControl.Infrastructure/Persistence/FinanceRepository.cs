using AgroControl.Application.Finance;
using AgroControl.Domain.Modules.Finance;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class FinanceRepository(AgroControlDbContext dbContext) : IFinanceRepository
{
    public async Task<(IReadOnlyList<FinancialCategory> Items, int TotalCount)> ListCategoriesAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.FinancialCategories.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<FinancialCategory?> GetCategoryAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<FinancialCategory> query = dbContext.FinancialCategories.Where(x => x.OrganizationId == organizationId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CategoryNameExistsAsync(Guid organizationId, string name, Guid? excludingId, CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLower();
        return dbContext.FinancialCategories.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Name.ToLower() == normalized && (excludingId == null || x.Id != excludingId), cancellationToken);
    }

    public void AddCategory(FinancialCategory category) => dbContext.FinancialCategories.Add(category);

    public async Task<(IReadOnlyList<CostCenter> Items, int TotalCount)> ListCostCentersAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CostCenters.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<CostCenter?> GetCostCenterAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<CostCenter> query = dbContext.CostCenters.Where(x => x.OrganizationId == organizationId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CostCenterNameExistsAsync(Guid organizationId, string name, Guid? excludingId, CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLower();
        return dbContext.CostCenters.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Name.ToLower() == normalized && (excludingId == null || x.Id != excludingId), cancellationToken);
    }

    public void AddCostCenter(CostCenter costCenter) => dbContext.CostCenters.Add(costCenter);

    public async Task<(IReadOnlyList<FinancialTransaction> Items, int TotalCount)> ListTransactionsAsync(
        Guid organizationId, int skip, int take, FinancialEntryType? type, FinancialStatus? status,
        DateOnly? from, DateOnly? to, Guid? farmId, Guid? fieldId, Guid? seasonId, string? search,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(dbContext.FinancialTransactions.AsNoTracking(), organizationId, from, to, farmId, fieldId, seasonId);
        if (type is not null) query = query.Where(x => x.Type == type);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Description.ToLower().Contains(term) || (x.Counterparty != null && x.Counterparty.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CompetenceDate).ThenByDescending(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<FinancialTransaction?> GetTransactionAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<FinancialTransaction> query = dbContext.FinancialTransactions.Where(x => x.OrganizationId == organizationId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public void AddTransaction(FinancialTransaction transaction) => dbContext.FinancialTransactions.Add(transaction);

    public async Task<FinanceTotals> GetTotalsAsync(
        Guid organizationId, DateOnly? from, DateOnly? to, Guid? farmId, Guid? fieldId, Guid? seasonId,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(dbContext.FinancialTransactions.AsNoTracking(), organizationId, from, to, farmId, fieldId, seasonId)
            .Where(x => x.Status != FinancialStatus.Cancelled);

        var accruedRevenue = await query.Where(x => x.Type == FinancialEntryType.Revenue).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var accruedExpense = await query.Where(x => x.Type == FinancialEntryType.Expense).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var cashRevenue = await query.Where(x => x.Type == FinancialEntryType.Revenue && x.Status == FinancialStatus.Received).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var cashExpense = await query.Where(x => x.Type == FinancialEntryType.Expense && x.Status == FinancialStatus.Paid).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var pendingReceivables = await query.Where(x => x.Type == FinancialEntryType.Revenue && x.Status == FinancialStatus.Pending).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var pendingPayables = await query.Where(x => x.Type == FinancialEntryType.Expense && x.Status == FinancialStatus.Pending).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        return new FinanceTotals(accruedRevenue, accruedExpense, cashRevenue, cashExpense, pendingReceivables, pendingPayables);
    }

    private static IQueryable<FinancialTransaction> ApplyFilters(
        IQueryable<FinancialTransaction> query, Guid organizationId, DateOnly? from, DateOnly? to,
        Guid? farmId, Guid? fieldId, Guid? seasonId)
    {
        query = query.Where(x => x.OrganizationId == organizationId);
        if (from is not null) query = query.Where(x => x.CompetenceDate >= from.Value);
        if (to is not null) query = query.Where(x => x.CompetenceDate <= to.Value);
        if (farmId is not null) query = query.Where(x => x.FarmId == farmId);
        if (fieldId is not null) query = query.Where(x => x.FieldId == fieldId);
        if (seasonId is not null) query = query.Where(x => x.SeasonId == seasonId);
        return query;
    }
}
