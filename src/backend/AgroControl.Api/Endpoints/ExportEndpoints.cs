using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Exporting;
using AgroControl.Domain.Modules.Exporting;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/export").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Export));

        group.MapGet("/orders", async (ClaimsPrincipal user, ExportService service, int page = 1, int pageSize = 20,
            string? search = null, ExportOrderStatus? status = null, string? countryCode = null, Guid? seasonId = null,
            DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default) =>
            Results.Ok(await service.ListOrdersAsync(GetOrganizationId(user), page, pageSize, search, status, countryCode, seasonId, from, to, ct)));

        group.MapGet("/orders/{id:guid}", async (Guid id, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var order = await service.GetOrderAsync(GetOrganizationId(user), id, ct);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapPost("/orders", async (CreateExportOrderCommand command, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.CreateOrderAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/export/orders/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/orders/{id:guid}", async (Guid id, UpdateExportOrderCommand command, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.UpdateOrderAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/orders/{id:guid}/status", async (Guid id, TransitionExportOrderStatusCommand command, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.TransitionStatusAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/orders/{id:guid}/timeline", async (Guid id, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.GetTimelineAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/orders/{id:guid}/documents", async (Guid id, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.ListDocumentsAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/orders/{id:guid}/documents", async (Guid id, CreateExportDocumentCommand command, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.CreateDocumentAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/export/orders/{id}/documents/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/orders/{orderId:guid}/documents/{documentId:guid}", async (Guid orderId, Guid documentId,
            UpdateExportDocumentCommand command, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.UpdateDocumentAsync(GetOrganizationId(user), orderId, documentId, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/orders/{id:guid}/costs", async (Guid id, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.ListCostsAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/orders/{id:guid}/costs", async (Guid id, CreateExportCostCommand command, ClaimsPrincipal user, ExportService service, CancellationToken ct) =>
        {
            var result = await service.AddCostAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/export/orders/{id}/costs/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapGet("/summary", async (ClaimsPrincipal user, ExportService service, DateOnly? from = null, DateOnly? to = null,
            ExportOrderStatus? status = null, string? countryCode = null, string? currency = null, CancellationToken ct = default) =>
            Results.Ok(await service.GetSummaryAsync(GetOrganizationId(user), from, to, status, countryCode, currency, ct)));

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
