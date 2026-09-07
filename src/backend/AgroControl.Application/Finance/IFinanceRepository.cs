using AgroControl.Domain.Modules.Finance;

namespace AgroControl.Application.Finance;

public sealed record FinanceTotals(
    decimal AccruedRevenue,
    decimal AccruedExpense,
    decimal CashRevenue,
    decimal CashExpense,
    decimal PendingReceivables,
    decimal PendingPayables);

public interface IFinanceRepository
{
    Task<(IReadOnlyList<FinancialCategory> Items, int TotalCount)> ListCategoriesAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<FinancialCategory?> GetCategoryAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> CategoryNameExistsAsync(Guid organizationId, string name, Guid? excludingId, CancellationToken cancellationToken = default);
    void AddCategory(FinancialCategory category);

    Task<(IReadOnlyList<CostCenter> Items, int TotalCount)> ListCostCentersAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<CostCenter?> GetCostCenterAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> CostCenterNameExistsAsync(Guid organizationId, string name, Guid? excludingId, CancellationToken cancellationToken = default);
    void AddCostCenter(CostCenter costCenter);

    Task<(IReadOnlyList<FinancialTransaction> Items, int TotalCount)> ListTransactionsAsync(
        Guid organizationId,
        int skip,
        int take,
        FinancialEntryType? type,
        FinancialStatus? status,
        DateOnly? from,
        DateOnly? to,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? search,
        CancellationToken cancellationToken = default);
    Task<FinancialTransaction?> GetTransactionAsync(Guid organizationId, Guid id, bool tracking, CancellationToken cancellationToken = default);
    void AddTransaction(FinancialTransaction transaction);

    Task<FinanceTotals> GetTotalsAsync(
        Guid organizationId,
        DateOnly? from,
        DateOnly? to,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        CancellationToken cancellationToken = default);
}
