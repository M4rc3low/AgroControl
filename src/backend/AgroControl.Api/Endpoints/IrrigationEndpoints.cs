using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Irrigation;
using AgroControl.Application.Telemetry;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class IrrigationEndpoints
{
    public static IEndpointRouteBuilder MapIrrigationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/irrigation").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Irrigation));

        group.MapGet("/zones", async (
            ClaimsPrincipal user, IrrigationService service, int page = 1, int pageSize = 20,
            Guid? fieldId = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListZonesAsync(GetOrganizationId(user), page, pageSize, fieldId, includeInactive, ct)));

        group.MapGet("/zones/{id:guid}", async (Guid id, ClaimsPrincipal user, IrrigationService service, CancellationToken ct) =>
        {
            var item = await service.GetZoneAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/zones", async (CreateIrrigationZoneCommand command, ClaimsPrincipal user, IrrigationService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.CreateZoneAsync(GetOrganizationId(user), command, ct);
                return result.Succeeded ? Results.Created($"/api/v1/irrigation/zones/{result.Value!.Id}", result.Value) : ToError(result);
            }
            catch (TelemetryClientException ex) { return TelemetryError(ex); }
        });

        group.MapPut("/zones/{id:guid}", async (Guid id, UpdateIrrigationZoneCommand command, ClaimsPrincipal user, IrrigationService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.UpdateZoneAsync(GetOrganizationId(user), id, command, ct);
                return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
            }
            catch (TelemetryClientException ex) { return TelemetryError(ex); }
        });

        group.MapDelete("/zones/{id:guid}", async (Guid id, ClaimsPrincipal user, IrrigationService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateZoneAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/zones/{id:guid}/status", async (Guid id, ClaimsPrincipal user, IrrigationService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.GetZoneStatusAsync(GetOrganizationId(user), id, ct);
                return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
            }
            catch (TelemetryClientException ex) { return TelemetryError(ex); }
        });

        group.MapGet("/applications", async (
            ClaimsPrincipal user, IrrigationService service, int page = 1, int pageSize = 20,
            Guid? fieldId = null, Guid? zoneId = null, DateTime? fromUtc = null, DateTime? toUtc = null,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListApplicationsAsync(GetOrganizationId(user), page, pageSize, fieldId, zoneId, fromUtc, toUtc, ct)));

        group.MapPost("/applications", async (CreateIrrigationApplicationCommand command, ClaimsPrincipal user, IrrigationService service, CancellationToken ct) =>
        {
            var result = await service.CreateApplicationAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/irrigation/applications/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapGet("/summary", async (
            ClaimsPrincipal user, IrrigationService service, Guid? fieldId = null, Guid? zoneId = null,
            DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default) =>
            Results.Ok(await service.GetSummaryAsync(GetOrganizationId(user), fieldId, zoneId, fromUtc, toUtc, ct)));

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

    private static IResult TelemetryError(TelemetryClientException exception) => exception.Kind switch
    {
        TelemetryClientErrorKind.Timeout => Results.Problem(statusCode: 504, title: "Telemetry timeout", detail: exception.Message),
        TelemetryClientErrorKind.Unavailable => Results.Problem(statusCode: 503, title: "Telemetry unavailable", detail: exception.Message),
        TelemetryClientErrorKind.NotFound => Results.NotFound(new { message = exception.Message }),
        TelemetryClientErrorKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["telemetry"] = [exception.Message] }),
        TelemetryClientErrorKind.Conflict => Results.Conflict(new { message = exception.Message }),
        _ => Results.Problem(statusCode: 502, title: "Telemetry error", detail: exception.Message)
    };
}
