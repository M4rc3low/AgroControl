using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Machinery;
using AgroControl.Domain.Modules.Machinery;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class MachineryEndpoints
{
    public static IEndpointRouteBuilder MapMachineryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/machinery").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Machinery));

        group.MapGet("/machines", async (
            ClaimsPrincipal user,
            MachineryService service,
            int page = 1,
            int pageSize = 20,
            MachineStatus? status = null,
            Guid? farmId = null,
            string? search = null,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListMachinesAsync(GetOrganizationId(user), page, pageSize, status, farmId, search, ct)));

        group.MapGet("/machines/{id:guid}", async (Guid id, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var item = await service.GetMachineAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/machines", async (CreateMachineCommand command, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.CreateMachineAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/machinery/machines/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/machines/{id:guid}", async (Guid id, UpdateMachineCommand command, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.UpdateMachineAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/machines/{id:guid}", async (Guid id, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateMachineAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapPost("/machines/{id:guid}/hour-meter", async (
            Guid id, RecordHourMeterCommand command, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.RecordHourMeterAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/machinery/machines/{id}/hour-meter", result.Value) : ToError(result);
        });

        group.MapGet("/machines/{id:guid}/hour-meter", async (
            Guid id, ClaimsPrincipal user, MachineryService service, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        {
            var result = await service.ListHourMeterAsync(GetOrganizationId(user), id, page, pageSize, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/machines/{id:guid}/fuelings", async (
            Guid id, CreateFuelingCommand command, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.AddFuelingAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/machinery/machines/{id}/fuelings", result.Value) : ToError(result);
        });

        group.MapGet("/machines/{id:guid}/fuelings", async (
            Guid id, ClaimsPrincipal user, MachineryService service, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        {
            var result = await service.ListFuelingsAsync(GetOrganizationId(user), id, page, pageSize, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/machines/{id:guid}/maintenance", async (
            Guid id, CreateMaintenanceCommand command, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.AddMaintenanceAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/machinery/machines/{id}/maintenance", result.Value) : ToError(result);
        });

        group.MapGet("/machines/{id:guid}/maintenance", async (
            Guid id,
            ClaimsPrincipal user,
            MachineryService service,
            int page = 1,
            int pageSize = 20,
            MaintenanceKind? kind = null,
            CancellationToken ct = default) =>
        {
            var result = await service.ListMaintenanceAsync(GetOrganizationId(user), id, page, pageSize, kind, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/machines/{id:guid}/cost-summary", async (
            Guid id, ClaimsPrincipal user, MachineryService service, CancellationToken ct) =>
        {
            var result = await service.GetCostSummaryAsync(GetOrganizationId(user), id, ct);
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
