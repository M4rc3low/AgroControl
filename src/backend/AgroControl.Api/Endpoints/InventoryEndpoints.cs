using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Inventory;
using AgroControl.Domain.Modules.Inventory;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/inventory").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Inventory));

        group.MapGet("/categories", async (ClaimsPrincipal user, InventoryService service, int page = 1, int pageSize = 20, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListCategoriesAsync(GetOrganizationId(user), page, pageSize, search, includeInactive, ct)));
        group.MapPost("/categories", async (CreateInventoryCategoryCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateCategoryAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/inventory/categories/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/categories/{id:guid}", async (Guid id, UpdateInventoryCategoryCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.UpdateCategoryAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/categories/{id:guid}", async (Guid id, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateCategoryAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/items", async (ClaimsPrincipal user, InventoryService service, int page = 1, int pageSize = 20, Guid? categoryId = null, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListItemsAsync(GetOrganizationId(user), page, pageSize, categoryId, search, includeInactive, ct)));
        group.MapGet("/items/{id:guid}", async (Guid id, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var item = await service.GetItemAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        group.MapPost("/items", async (CreateInventoryItemCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateItemAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/inventory/items/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/items/{id:guid}", async (Guid id, UpdateInventoryItemCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.UpdateItemAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/items/{id:guid}", async (Guid id, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateItemAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/warehouses", async (ClaimsPrincipal user, InventoryService service, int page = 1, int pageSize = 20, Guid? farmId = null, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListWarehousesAsync(GetOrganizationId(user), page, pageSize, farmId, search, includeInactive, ct)));
        group.MapPost("/warehouses", async (CreateWarehouseCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateWarehouseAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/inventory/warehouses/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/warehouses/{id:guid}", async (Guid id, UpdateWarehouseCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.UpdateWarehouseAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/warehouses/{id:guid}", async (Guid id, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateWarehouseAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/movements", async (ClaimsPrincipal user, InventoryService service, int page = 1, int pageSize = 20, Guid? itemId = null, Guid? warehouseId = null, StockMovementType? type = null, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default) =>
            Results.Ok(await service.ListMovementsAsync(GetOrganizationId(user), page, pageSize, itemId, warehouseId, type, fromUtc, toUtc, ct)));
        group.MapPost("/movements", async (CreateStockMovementCommand command, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateMovementAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/inventory/movements/{result.Value!.Movement.Id}", result.Value) : ToError(result);
        });

        group.MapGet("/items/{itemId:guid}/warehouses/{warehouseId:guid}/balance", async (Guid itemId, Guid warehouseId, ClaimsPrincipal user, InventoryService service, CancellationToken ct) =>
            Results.Ok(await service.GetBalanceAsync(GetOrganizationId(user), itemId, warehouseId, ct)));
        group.MapGet("/low-stock", async (ClaimsPrincipal user, InventoryService service, int take = 100, CancellationToken ct = default) =>
            Results.Ok(await service.ListLowStockAsync(GetOrganizationId(user), take, ct)));

        return endpoints;
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("org_id")!);

    private static IResult ToError<T>(OperationResult<T> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error ?? "Invalid request."] }),
        OperationErrorKind.NotFound => Results.NotFound(new { message = result.Error }),
        OperationErrorKind.Conflict => Results.Conflict(new { message = result.Error }),
        _ => Results.Problem(statusCode: 500, title: "Operation failed", detail: result.Error)
    };
}
