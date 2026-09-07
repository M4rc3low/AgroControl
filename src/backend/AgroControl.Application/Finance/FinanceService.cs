using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Finance;

namespace AgroControl.Application.Finance;

public sealed class FinanceService(
    IFinanceRepository repository,
    IProductionRepository productionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<FinancialCategoryDto>> ListCategoriesAsync(Guid organizationId, int page, int pageSize, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListCategoriesAsync(organizationId, (page - 1) * pageSize, pageSize, search, includeInactive, cancellationToken);
        return new PagedResult<FinancialCategoryDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<FinancialCategoryDto>> CreateCategoryAsync(Guid organizationId, CreateFinancialCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return OperationResult<FinancialCategoryDto>.Validation("Category name is required.");
        if (await repository.CategoryNameExistsAsync(organizationId, command.Name.Trim(), null, cancellationToken))
            return OperationResult<FinancialCategoryDto>.Conflict("A financial category with this name already exists.");

        var category = FinancialCategory.Create(organizationId, command.Name, DateTime.UtcNow);
        repository.AddCategory(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FinancialCategoryDto>.Success(ToDto(category));
    }

    public async Task<OperationResult<FinancialCategoryDto>> UpdateCategoryAsync(Guid organizationId, Guid id, UpdateFinancialCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetCategoryAsync(organizationId, id, true, cancellationToken);
        if (category is null) return OperationResult<FinancialCategoryDto>.NotFound("Financial category not found.");
        if (string.IsNullOrWhiteSpace(command.Name)) return OperationResult<FinancialCategoryDto>.Validation("Category name is required.");
        if (await repository.CategoryNameExistsAsync(organizationId, command.Name.Trim(), id, cancellationToken))
            return OperationResult<FinancialCategoryDto>.Conflict("A financial category with this name already exists.");

        category.Update(command.Name, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FinancialCategoryDto>.Success(ToDto(category));
    }

    public async Task<OperationResult<bool>> DeactivateCategoryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetCategoryAsync(organizationId, id, true, cancellationToken);
        if (category is null) return OperationResult<bool>.NotFound("Financial category not found.");
        category.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<PagedResult<CostCenterDto>> ListCostCentersAsync(Guid organizationId, int page, int pageSize, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListCostCentersAsync(organizationId, (page - 1) * pageSize, pageSize, search, includeInactive, cancellationToken);
        return new PagedResult<CostCenterDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<CostCenterDto>> CreateCostCenterAsync(Guid organizationId, CreateCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return OperationResult<CostCenterDto>.Validation("Cost center name is required.");
        if (await repository.CostCenterNameExistsAsync(organizationId, command.Name.Trim(), null, cancellationToken))
            return OperationResult<CostCenterDto>.Conflict("A cost center with this name already exists.");

        var item = CostCenter.Create(organizationId, command.Name, DateTime.UtcNow);
        repository.AddCostCenter(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<CostCenterDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<CostCenterDto>> UpdateCostCenterAsync(Guid organizationId, Guid id, UpdateCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetCostCenterAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<CostCenterDto>.NotFound("Cost center not found.");
        if (string.IsNullOrWhiteSpace(command.Name)) return OperationResult<CostCenterDto>.Validation("Cost center name is required.");
        if (await repository.CostCenterNameExistsAsync(organizationId, command.Name.Trim(), id, cancellationToken))
            return OperationResult<CostCenterDto>.Conflict("A cost center with this name already exists.");

        item.Update(command.Name, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<CostCenterDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<bool>> DeactivateCostCenterAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetCostCenterAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<bool>.NotFound("Cost center not found.");
        item.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<PagedResult<FinancialTransactionDto>> ListTransactionsAsync(
        Guid organizationId, int page, int pageSize, FinancialEntryType? type, FinancialStatus? status,
        DateOnly? from, DateOnly? to, Guid? farmId, Guid? fieldId, Guid? seasonId, string? search,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListTransactionsAsync(
            organizationId, (page - 1) * pageSize, pageSize, type, status, from, to,
            farmId, fieldId, seasonId, search, cancellationToken);
        return new PagedResult<FinancialTransactionDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<FinancialTransactionDto?> GetTransactionAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetTransactionAsync(organizationId, id, false, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<OperationResult<FinancialTransactionDto>> CreateTransactionAsync(Guid organizationId, CreateFinancialTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var refs = await ValidateAndNormalizeAsync(organizationId, command.CategoryId, command.CostCenterId, command.FarmId, command.FieldId, command.SeasonId, command.Description, command.Amount, cancellationToken);
        if (refs.Error is not null) return OperationResult<FinancialTransactionDto>.Validation(refs.Error);

        FinancialTransaction item;
        try
        {
            item = FinancialTransaction.Create(
                organizationId, command.CategoryId, command.CostCenterId, command.Type, command.Description,
                command.Counterparty, command.Amount, command.CompetenceDate, command.DueDate,
                refs.FarmId, refs.FieldId, refs.SeasonId, command.Notes, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<FinancialTransactionDto>.Validation(ex.Message);
        }

        repository.AddTransaction(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FinancialTransactionDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<FinancialTransactionDto>> UpdateTransactionAsync(Guid organizationId, Guid id, UpdateFinancialTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetTransactionAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<FinancialTransactionDto>.NotFound("Financial transaction not found.");
        if (item.Status != FinancialStatus.Pending) return OperationResult<FinancialTransactionDto>.Conflict("Only pending financial transactions can be edited.");

        var refs = await ValidateAndNormalizeAsync(organizationId, command.CategoryId, command.CostCenterId, command.FarmId, command.FieldId, command.SeasonId, command.Description, command.Amount, cancellationToken);
        if (refs.Error is not null) return OperationResult<FinancialTransactionDto>.Validation(refs.Error);

        try
        {
            item.Update(
                command.CategoryId, command.CostCenterId, command.Type, command.Description, command.Counterparty,
                command.Amount, command.CompetenceDate, command.DueDate, refs.FarmId, refs.FieldId, refs.SeasonId,
                command.Notes, DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<FinancialTransactionDto>.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<FinancialTransactionDto>.Validation(ex.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FinancialTransactionDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<FinancialTransactionDto>> SettleTransactionAsync(Guid organizationId, Guid id, SettleFinancialTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetTransactionAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<FinancialTransactionDto>.NotFound("Financial transaction not found.");

        try
        {
            item.Settle(command.SettledOn ?? DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<FinancialTransactionDto>.Conflict(ex.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FinancialTransactionDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<FinancialTransactionDto>> CancelTransactionAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetTransactionAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<FinancialTransactionDto>.NotFound("Financial transaction not found.");

        try
        {
            item.Cancel(DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<FinancialTransactionDto>.Conflict(ex.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<FinancialTransactionDto>.Success(ToDto(item));
    }

    public async Task<FinancialSummaryDto> GetSummaryAsync(
        Guid organizationId, DateOnly? from, DateOnly? to, Guid? farmId, Guid? fieldId, Guid? seasonId,
        CancellationToken cancellationToken = default)
    {
        var totals = await repository.GetTotalsAsync(organizationId, from, to, farmId, fieldId, seasonId, cancellationToken);
        return ToSummary(totals);
    }

    public async Task<OperationResult<SeasonFinancialSummaryDto>> GetSeasonSummaryAsync(Guid organizationId, Guid seasonId, CancellationToken cancellationToken = default)
    {
        var season = await productionRepository.GetSeasonAsync(organizationId, seasonId, false, cancellationToken);
        if (season is null) return OperationResult<SeasonFinancialSummaryDto>.NotFound("Season not found.");
        var field = await productionRepository.GetFieldAsync(organizationId, season.FieldId, false, cancellationToken);
        if (field is null) return OperationResult<SeasonFinancialSummaryDto>.NotFound("Season field not found.");

        var totals = await repository.GetTotalsAsync(organizationId, null, null, null, null, seasonId, cancellationToken);
        var result = totals.AccruedRevenue - totals.AccruedExpense;
        var margin = totals.AccruedRevenue > 0 ? result / totals.AccruedRevenue * 100m : null;
        var productionUnits = season.ActualYieldPerHectare is > 0 ? season.ActualYieldPerHectare.Value * field.AreaHectares : null;
        var costPerHectare = field.AreaHectares > 0 ? totals.AccruedExpense / field.AreaHectares : 0m;
        var costPerUnit = productionUnits is > 0 ? totals.AccruedExpense / productionUnits.Value : null;

        var dto = new SeasonFinancialSummaryDto(
            season.Id, field.Id, field.FarmId, field.AreaHectares, season.ActualYieldPerHectare, productionUnits,
            totals.AccruedRevenue, totals.AccruedExpense, result, margin, costPerHectare, costPerUnit, costPerUnit,
            totals.CashRevenue, totals.CashExpense, totals.PendingReceivables, totals.PendingPayables);
        return OperationResult<SeasonFinancialSummaryDto>.Success(dto);
    }

    private async Task<NormalizedReferences> ValidateAndNormalizeAsync(
        Guid organizationId, Guid? categoryId, Guid? costCenterId, Guid? farmId, Guid? fieldId, Guid? seasonId,
        string description, decimal amount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(description)) return NormalizedReferences.Fail("Description is required.");
        if (amount <= 0) return NormalizedReferences.Fail("Amount must be greater than zero.");

        if (categoryId is not null)
        {
            var category = await repository.GetCategoryAsync(organizationId, categoryId.Value, false, cancellationToken);
            if (category is null || !category.IsActive) return NormalizedReferences.Fail("Financial category does not belong to this organization or is inactive.");
        }
        if (costCenterId is not null)
        {
            var costCenter = await repository.GetCostCenterAsync(organizationId, costCenterId.Value, false, cancellationToken);
            if (costCenter is null || !costCenter.IsActive) return NormalizedReferences.Fail("Cost center does not belong to this organization or is inactive.");
        }

        Guid? normalizedFarmId = farmId;
        Guid? normalizedFieldId = fieldId;

        if (seasonId is not null)
        {
            var season = await productionRepository.GetSeasonAsync(organizationId, seasonId.Value, false, cancellationToken);
            if (season is null) return NormalizedReferences.Fail("Season does not belong to this organization.");
            if (fieldId is not null && fieldId.Value != season.FieldId) return NormalizedReferences.Fail("Season does not belong to the informed field.");
            normalizedFieldId = season.FieldId;
        }

        if (normalizedFieldId is not null)
        {
            var field = await productionRepository.GetFieldAsync(organizationId, normalizedFieldId.Value, false, cancellationToken);
            if (field is null) return NormalizedReferences.Fail("Field does not belong to this organization.");
            if (farmId is not null && farmId.Value != field.FarmId) return NormalizedReferences.Fail("Field does not belong to the informed farm.");
            normalizedFarmId = field.FarmId;
        }

        if (normalizedFarmId is not null)
        {
            var farm = await productionRepository.GetFarmAsync(organizationId, normalizedFarmId.Value, false, cancellationToken);
            if (farm is null) return NormalizedReferences.Fail("Farm does not belong to this organization.");
        }

        return new NormalizedReferences(normalizedFarmId, normalizedFieldId, seasonId, null);
    }

    private static FinancialSummaryDto ToSummary(FinanceTotals totals)
    {
        var accruedResult = totals.AccruedRevenue - totals.AccruedExpense;
        var margin = totals.AccruedRevenue > 0 ? accruedResult / totals.AccruedRevenue * 100m : null;
        return new FinancialSummaryDto(
            totals.AccruedRevenue, totals.AccruedExpense, accruedResult, margin,
            totals.CashRevenue, totals.CashExpense, totals.CashRevenue - totals.CashExpense,
            totals.PendingReceivables, totals.PendingPayables);
    }

    private static FinancialCategoryDto ToDto(FinancialCategory item) => new(item.Id, item.Name, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static CostCenterDto ToDto(CostCenter item) => new(item.Id, item.Name, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static FinancialTransactionDto ToDto(FinancialTransaction item) => new(
        item.Id, item.CategoryId, item.CostCenterId, item.Type, item.Status, item.Description, item.Counterparty,
        item.Amount, item.CompetenceDate, item.DueDate, item.SettledOn, item.FarmId, item.FieldId, item.SeasonId,
        item.Notes, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private sealed record NormalizedReferences(Guid? FarmId, Guid? FieldId, Guid? SeasonId, string? Error)
    {
        public static NormalizedReferences Fail(string error) => new(null, null, null, error);
    }
}
