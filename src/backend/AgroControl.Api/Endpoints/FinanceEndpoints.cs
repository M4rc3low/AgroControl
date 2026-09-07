using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Finance;
using AgroControl.Domain.Modules.Finance;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/finance").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Finance));

        group.MapGet("/categories", async (ClaimsPrincipal user, FinanceService service, int page = 1, int pageSize = 20, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListCategoriesAsync(GetOrganizationId(user), page, pageSize, search, includeInactive, ct)));
        group.MapPost("/categories", async (CreateFinancialCategoryCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.CreateCategoryAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/finance/categories/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/categories/{id:guid}", async (Guid id, UpdateFinancialCategoryCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.UpdateCategoryAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/categories/{id:guid}", async (Guid id, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateCategoryAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/cost-centers", async (ClaimsPrincipal user, FinanceService service, int page = 1, int pageSize = 20, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListCostCentersAsync(GetOrganizationId(user), page, pageSize, search, includeInactive, ct)));
        group.MapPost("/cost-centers", async (CreateCostCenterCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.CreateCostCenterAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/finance/cost-centers/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/cost-centers/{id:guid}", async (Guid id, UpdateCostCenterCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.UpdateCostCenterAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/cost-centers/{id:guid}", async (Guid id, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateCostCenterAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/transactions", async (
            ClaimsPrincipal user, FinanceService service, int page = 1, int pageSize = 20,
            FinancialEntryType? type = null, FinancialStatus? status = null, DateOnly? from = null, DateOnly? to = null,
            Guid? farmId = null, Guid? fieldId = null, Guid? seasonId = null, string? search = null, CancellationToken ct = default) =>
            Results.Ok(await service.ListTransactionsAsync(GetOrganizationId(user), page, pageSize, type, status, from, to, farmId, fieldId, seasonId, search, ct)));
        group.MapGet("/transactions/{id:guid}", async (Guid id, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var item = await service.GetTransactionAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        group.MapPost("/transactions", async (CreateFinancialTransactionCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.CreateTransactionAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/finance/transactions/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/transactions/{id:guid}", async (Guid id, UpdateFinancialTransactionCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.UpdateTransactionAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapPost("/transactions/{id:guid}/settle", async (Guid id, SettleFinancialTransactionCommand command, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.SettleTransactionAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapPost("/transactions/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.CancelTransactionAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/summary", async (
            ClaimsPrincipal user, FinanceService service, DateOnly? from = null, DateOnly? to = null,
            Guid? farmId = null, Guid? fieldId = null, Guid? seasonId = null, CancellationToken ct = default) =>
            Results.Ok(await service.GetSummaryAsync(GetOrganizationId(user), from, to, farmId, fieldId, seasonId, ct)));
        group.MapGet("/seasons/{seasonId:guid}/summary", async (Guid seasonId, ClaimsPrincipal user, FinanceService service, CancellationToken ct) =>
        {
            var result = await service.GetSeasonSummaryAsync(GetOrganizationId(user), seasonId, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

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
