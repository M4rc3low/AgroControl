using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Market;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class MarketEndpoints
{
    public static IEndpointRouteBuilder MapMarketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/market").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Market));

        group.MapGet("/commodities", async (
            ClaimsPrincipal user,
            MarketService service,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            bool includeInactive = false,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListCommoditiesAsync(GetOrganizationId(user), page, pageSize, search, includeInactive, ct)));

        group.MapGet("/commodities/{id:guid}", async (Guid id, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var item = await service.GetCommodityAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/commodities", async (CreateCommodityCommand command, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.CreateCommodityAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/market/commodities/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/commodities/{id:guid}", async (Guid id, UpdateCommodityCommand command, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.UpdateCommodityAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/commodities/{id:guid}", async (Guid id, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateCommodityAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapPost("/commodities/{id:guid}/quotes", async (
            Guid id, AddMarketQuoteCommand command, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.AddQuoteAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/market/commodities/{id}/quotes", result.Value) : ToError(result);
        });

        group.MapGet("/commodities/{id:guid}/quotes", async (
            Guid id,
            ClaimsPrincipal user,
            MarketService service,
            int page = 1,
            int pageSize = 50,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken ct = default) =>
        {
            var result = await service.ListQuotesAsync(GetOrganizationId(user), id, page, pageSize, fromUtc, toUtc, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/commodities/{id:guid}/summary", async (
            Guid id, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.GetSummaryAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/commodities/{id:guid}/alerts", async (
            Guid id, CreatePriceAlertCommand command, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.CreateAlertAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/market/alerts/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapGet("/alerts", async (
            ClaimsPrincipal user,
            MarketService service,
            int page = 1,
            int pageSize = 20,
            Guid? commodityId = null,
            bool includeInactive = false,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListAlertsAsync(GetOrganizationId(user), page, pageSize, commodityId, includeInactive, ct)));

        group.MapDelete("/alerts/{id:guid}", async (Guid id, ClaimsPrincipal user, MarketService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateAlertAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
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
