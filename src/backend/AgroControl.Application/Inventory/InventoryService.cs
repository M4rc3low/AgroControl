using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Inventory;

namespace AgroControl.Application.Inventory;

public sealed class InventoryService(
    IInventoryRepository repository,
    IProductionRepository productionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<InventoryCategoryDto>> ListCategoriesAsync(Guid organizationId, int page, int pageSize, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListCategoriesAsync(organizationId, (page - 1) * pageSize, pageSize, search, includeInactive, cancellationToken);
        return new PagedResult<InventoryCategoryDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<InventoryCategoryDto>> CreateCategoryAsync(Guid organizationId, CreateInventoryCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return OperationResult<InventoryCategoryDto>.Validation("Category name is required.");
        if (await repository.CategoryNameExistsAsync(organizationId, command.Name.Trim(), null, cancellationToken))
            return OperationResult<InventoryCategoryDto>.Conflict("A category with this name already exists.");

        var category = InventoryCategory.Create(organizationId, command.Name, DateTime.UtcNow);
        repository.AddCategory(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<InventoryCategoryDto>.Success(ToDto(category));
    }

    public async Task<OperationResult<InventoryCategoryDto>> UpdateCategoryAsync(Guid organizationId, Guid id, UpdateInventoryCategoryCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return OperationResult<InventoryCategoryDto>.Validation("Category name is required.");
        var category = await repository.GetCategoryAsync(organizationId, id, true, cancellationToken);
        if (category is null) return OperationResult<InventoryCategoryDto>.NotFound("Category not found.");
        if (await repository.CategoryNameExistsAsync(organizationId, command.Name.Trim(), id, cancellationToken))
            return OperationResult<InventoryCategoryDto>.Conflict("A category with this name already exists.");

        category.Update(command.Name, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<InventoryCategoryDto>.Success(ToDto(category));
    }

    public async Task<OperationResult<bool>> DeactivateCategoryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetCategoryAsync(organizationId, id, true, cancellationToken);
        if (category is null) return OperationResult<bool>.NotFound("Category not found.");
        category.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<PagedResult<InventoryItemDto>> ListItemsAsync(Guid organizationId, int page, int pageSize, Guid? categoryId, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListItemsAsync(organizationId, (page - 1) * pageSize, pageSize, categoryId, search, includeInactive, cancellationToken);
        return new PagedResult<InventoryItemDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<InventoryItemDto?> GetItemAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemAsync(organizationId, id, false, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<OperationResult<InventoryItemDto>> CreateItemAsync(Guid organizationId, CreateInventoryItemCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateItemAsync(organizationId, command.CategoryId, command.Sku, command.Name, command.MinimumStock, null, cancellationToken);
        if (validation is not null) return validation;

        var item = InventoryItem.Create(organizationId, command.CategoryId, command.Sku, command.Name, command.Unit, command.MinimumStock, DateTime.UtcNow);
        repository.AddItem(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<InventoryItemDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<InventoryItemDto>> UpdateItemAsync(Guid organizationId, Guid id, UpdateInventoryItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<InventoryItemDto>.NotFound("Inventory item not found.");

        var validation = await ValidateItemAsync(organizationId, command.CategoryId, command.Sku, command.Name, command.MinimumStock, id, cancellationToken);
        if (validation is not null) return validation;

        item.Update(command.CategoryId, command.Sku, command.Name, command.Unit, command.MinimumStock, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<InventoryItemDto>.Success(ToDto(item));
    }

    public async Task<OperationResult<bool>> DeactivateItemAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemAsync(organizationId, id, true, cancellationToken);
        if (item is null) return OperationResult<bool>.NotFound("Inventory item not found.");
        item.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<PagedResult<WarehouseDto>> ListWarehousesAsync(Guid organizationId, int page, int pageSize, Guid? farmId, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListWarehousesAsync(organizationId, (page - 1) * pageSize, pageSize, farmId, search, includeInactive, cancellationToken);
        return new PagedResult<WarehouseDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<WarehouseDto>> CreateWarehouseAsync(Guid organizationId, CreateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateWarehouseAsync(organizationId, command.FarmId, command.Name, null, cancellationToken);
        if (validation is not null) return validation;

        var warehouse = Warehouse.Create(organizationId, command.FarmId, command.Name, command.Location, DateTime.UtcNow);
        repository.AddWarehouse(warehouse);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<WarehouseDto>.Success(ToDto(warehouse));
    }

    public async Task<OperationResult<WarehouseDto>> UpdateWarehouseAsync(Guid organizationId, Guid id, UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var warehouse = await repository.GetWarehouseAsync(organizationId, id, true, cancellationToken);
        if (warehouse is null) return OperationResult<WarehouseDto>.NotFound("Warehouse not found.");

        var validation = await ValidateWarehouseAsync(organizationId, command.FarmId, command.Name, id, cancellationToken);
        if (validation is not null) return validation;

        warehouse.Update(command.FarmId, command.Name, command.Location, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<WarehouseDto>.Success(ToDto(warehouse));
    }

    public async Task<OperationResult<bool>> DeactivateWarehouseAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await repository.GetWarehouseAsync(organizationId, id, true, cancellationToken);
        if (warehouse is null) return OperationResult<bool>.NotFound("Warehouse not found.");
        warehouse.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<PagedResult<StockMovementDto>> ListMovementsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        Guid? itemId,
        Guid? warehouseId,
        StockMovementType? type,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListMovementsAsync(organizationId, (page - 1) * pageSize, pageSize, itemId, warehouseId, type, fromUtc, toUtc, cancellationToken);
        return new PagedResult<StockMovementDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<OperationResult<StockMovementCreatedDto>> CreateMovementAsync(Guid organizationId, CreateStockMovementCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Quantity <= 0) return OperationResult<StockMovementCreatedDto>.Validation("Quantity must be greater than zero.");

        var item = await repository.GetItemAsync(organizationId, command.ItemId, false, cancellationToken);
        if (item is null || !item.IsActive) return OperationResult<StockMovementCreatedDto>.NotFound("Active inventory item not found.");

        var warehouse = await repository.GetWarehouseAsync(organizationId, command.WarehouseId, false, cancellationToken);
        if (warehouse is null || !warehouse.IsActive) return OperationResult<StockMovementCreatedDto>.NotFound("Active warehouse not found.");

        var relationError = await ValidateProductionReferencesAsync(organizationId, command.FarmId, command.FieldId, command.SeasonId, cancellationToken);
        if (relationError is not null) return OperationResult<StockMovementCreatedDto>.Validation(relationError);

        if (warehouse.FarmId is not null && command.FarmId is not null && warehouse.FarmId != command.FarmId)
            return OperationResult<StockMovementCreatedDto>.Validation("Movement farm must match the warehouse farm when both are informed.");

        var currentBalance = await repository.GetBalanceAsync(organizationId, command.ItemId, command.WarehouseId, cancellationToken);
        var removesStock = command.Type is StockMovementType.Exit or StockMovementType.AdjustmentNegative;
        if (removesStock && currentBalance < command.Quantity)
            return OperationResult<StockMovementCreatedDto>.Conflict($"Insufficient stock. Current balance is {currentBalance} {item.Unit}.");

        StockMovement movement;
        try
        {
            movement = StockMovement.Create(
                organizationId,
                command.ItemId,
                command.WarehouseId,
                command.Type,
                command.Quantity,
                command.OccurredAtUtc?.ToUniversalTime() ?? DateTime.UtcNow,
                command.BatchNumber,
                command.ExpirationDate,
                command.Notes,
                command.FarmId,
                command.FieldId,
                command.SeasonId,
                DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<StockMovementCreatedDto>.Validation(ex.Message);
        }

        repository.AddMovement(movement);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var result = new StockMovementCreatedDto(ToDto(movement), currentBalance + movement.SignedQuantity);
        return OperationResult<StockMovementCreatedDto>.Success(result);
    }

    public async Task<StockBalanceDto> GetBalanceAsync(Guid organizationId, Guid itemId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var balance = await repository.GetBalanceAsync(organizationId, itemId, warehouseId, cancellationToken);
        return new StockBalanceDto(itemId, warehouseId, balance);
    }

    public async Task<IReadOnlyList<LowStockItemDto>> ListLowStockAsync(Guid organizationId, int take = 100, CancellationToken cancellationToken = default)
    {
        var items = await repository.ListLowStockAsync(organizationId, Math.Clamp(take, 1, 500), cancellationToken);
        return items.Select(item => new LowStockItemDto(
            item.ItemId,
            item.Sku,
            item.Name,
            item.Unit,
            item.MinimumStock,
            item.CurrentStock,
            Math.Max(0m, item.MinimumStock - item.CurrentStock))).ToList();
    }

    private async Task<OperationResult<InventoryItemDto>?> ValidateItemAsync(Guid organizationId, Guid? categoryId, string sku, string name, decimal minimumStock, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name) || minimumStock < 0)
            return OperationResult<InventoryItemDto>.Validation("SKU and name are required; minimumStock cannot be negative.");

        if (categoryId is not null)
        {
            var category = await repository.GetCategoryAsync(organizationId, categoryId.Value, false, cancellationToken);
            if (category is null || !category.IsActive)
                return OperationResult<InventoryItemDto>.Validation("Category does not belong to this organization or is inactive.");
        }

        if (await repository.ItemSkuExistsAsync(organizationId, sku.Trim().ToUpperInvariant(), excludingId, cancellationToken))
            return OperationResult<InventoryItemDto>.Conflict("An inventory item with this SKU already exists.");

        return null;
    }

    private async Task<OperationResult<WarehouseDto>?> ValidateWarehouseAsync(Guid organizationId, Guid? farmId, string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return OperationResult<WarehouseDto>.Validation("Warehouse name is required.");
        if (farmId is not null)
        {
            var farm = await productionRepository.GetFarmAsync(organizationId, farmId.Value, false, cancellationToken);
            if (farm is null || !farm.IsActive) return OperationResult<WarehouseDto>.Validation("Farm does not belong to this organization or is inactive.");
        }
        if (await repository.WarehouseNameExistsAsync(organizationId, name.Trim(), excludingId, cancellationToken))
            return OperationResult<WarehouseDto>.Conflict("A warehouse with this name already exists.");
        return null;
    }

    private async Task<string?> ValidateProductionReferencesAsync(Guid organizationId, Guid? farmId, Guid? fieldId, Guid? seasonId, CancellationToken cancellationToken)
    {
        if (farmId is not null)
        {
            var farm = await productionRepository.GetFarmAsync(organizationId, farmId.Value, false, cancellationToken);
            if (farm is null) return "Farm does not belong to this organization.";
        }

        if (fieldId is not null)
        {
            var field = await productionRepository.GetFieldAsync(organizationId, fieldId.Value, false, cancellationToken);
            if (field is null) return "Field does not belong to this organization.";
            if (farmId is not null && field.FarmId != farmId) return "Field does not belong to the informed farm.";
        }

        if (seasonId is not null)
        {
            var season = await productionRepository.GetSeasonAsync(organizationId, seasonId.Value, false, cancellationToken);
            if (season is null) return "Season does not belong to this organization.";
            if (fieldId is not null && season.FieldId != fieldId) return "Season does not belong to the informed field.";
        }

        return null;
    }

    private static InventoryCategoryDto ToDto(InventoryCategory item) => new(item.Id, item.Name, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static InventoryItemDto ToDto(InventoryItem item) => new(item.Id, item.CategoryId, item.Sku, item.Name, item.Unit, item.MinimumStock, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static WarehouseDto ToDto(Warehouse item) => new(item.Id, item.FarmId, item.Name, item.Location, item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
    private static StockMovementDto ToDto(StockMovement item) => new(item.Id, item.ItemId, item.WarehouseId, item.Type, item.Quantity, item.SignedQuantity, item.OccurredAtUtc, item.BatchNumber, item.ExpirationDate, item.Notes, item.FarmId, item.FieldId, item.SeasonId, item.CreatedAtUtc);
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}
