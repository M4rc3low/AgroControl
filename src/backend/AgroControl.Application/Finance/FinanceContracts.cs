using AgroControl.Domain.Modules.Finance;

namespace AgroControl.Application.Finance;

public sealed record CreateFinancialCategoryCommand(string Name);
public sealed record UpdateFinancialCategoryCommand(string Name);
public sealed record FinancialCategoryDto(Guid Id, string Name, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateCostCenterCommand(string Name);
public sealed record UpdateCostCenterCommand(string Name);
public sealed record CostCenterDto(Guid Id, string Name, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateFinancialTransactionCommand(
    Guid? CategoryId,
    Guid? CostCenterId,
    FinancialEntryType Type,
    string Description,
    string? Counterparty,
    decimal Amount,
    DateOnly CompetenceDate,
    DateOnly? DueDate,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    string? Notes);

public sealed record UpdateFinancialTransactionCommand(
    Guid? CategoryId,
    Guid? CostCenterId,
    FinancialEntryType Type,
    string Description,
    string? Counterparty,
    decimal Amount,
    DateOnly CompetenceDate,
    DateOnly? DueDate,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    string? Notes);

public sealed record SettleFinancialTransactionCommand(DateOnly? SettledOn);

public sealed record FinancialTransactionDto(
    Guid Id,
    Guid? CategoryId,
    Guid? CostCenterId,
    FinancialEntryType Type,
    FinancialStatus Status,
    string Description,
    string? Counterparty,
    decimal Amount,
    DateOnly CompetenceDate,
    DateOnly? DueDate,
    DateOnly? SettledOn,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record FinancialSummaryDto(
    decimal AccruedRevenue,
    decimal AccruedExpense,
    decimal AccruedResult,
    decimal? AccruedMarginPercent,
    decimal CashRevenue,
    decimal CashExpense,
    decimal CashResult,
    decimal PendingReceivables,
    decimal PendingPayables);

public sealed record SeasonFinancialSummaryDto(
    Guid SeasonId,
    Guid FieldId,
    Guid FarmId,
    decimal AreaHectares,
    decimal? ActualYieldPerHectare,
    decimal? ProductionUnits,
    decimal Revenue,
    decimal Expense,
    decimal Result,
    decimal? MarginPercent,
    decimal CostPerHectare,
    decimal? CostPerUnit,
    decimal? BreakEvenPricePerUnit,
    decimal CashRevenue,
    decimal CashExpense,
    decimal PendingReceivables,
    decimal PendingPayables);
