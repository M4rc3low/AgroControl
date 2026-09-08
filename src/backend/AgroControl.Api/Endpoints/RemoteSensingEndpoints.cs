using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class RemoteSensingEndpoints
{
    public static IEndpointRouteBuilder MapRemoteSensingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/precision/remote-sensing").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.PrecisionAgriculture));

        group.MapGet("/scenes", async (ClaimsPrincipal user, RemoteSensingService service, int page = 1, int pageSize = 30,
            Guid? fieldId = null, Guid? seasonId = null, string? platform = null, string? provider = null,
            DateTime? fromUtc = null, DateTime? toUtc = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListScenesAsync(GetOrganizationId(user), page, pageSize, fieldId, seasonId, platform,
                provider, fromUtc, toUtc, includeInactive, ct)));

        group.MapGet("/scenes/{sceneId:guid}", async (Guid sceneId, ClaimsPrincipal user, RemoteSensingService service, CancellationToken ct) =>
        {
            var item = await service.GetSceneAsync(GetOrganizationId(user), sceneId, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/scenes", async (CreateRemoteSensingSceneCommand command, ClaimsPrincipal user, RemoteSensingService service, CancellationToken ct) =>
        {
            var result = await service.CreateSceneAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/precision/remote-sensing/scenes/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/scenes/{sceneId:guid}", async (Guid sceneId, UpdateRemoteSensingSceneCommand command, ClaimsPrincipal user, RemoteSensingService service, CancellationToken ct) =>
        {
            var result = await service.UpdateSceneAsync(GetOrganizationId(user), sceneId, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/scenes/{sceneId:guid}", async (Guid sceneId, ClaimsPrincipal user, RemoteSensingService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateSceneAsync(GetOrganizationId(user), sceneId, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/scenes/{sceneId:guid}/process", async (Guid sceneId, ProcessRasterSceneCommand command,
            ClaimsPrincipal user, RasterProcessingService service, CancellationToken ct) =>
        {
            var result = await service.ProcessSceneAsync(GetOrganizationId(user), sceneId, command, ct);
            return result.Kind switch
            {
                RasterProcessResultKind.Success => Results.Ok(result.Value),
                RasterProcessResultKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["raster"] = [result.Error ?? "Invalid raster-processing request."] }),
                RasterProcessResultKind.NotFound => Results.NotFound(new { message = result.Error }),
                RasterProcessResultKind.Conflict => Results.Conflict(new { message = result.Error }),
                RasterProcessResultKind.Timeout => Results.Problem(statusCode: 504, title: "Raster processing timed out", detail: result.Error),
                RasterProcessResultKind.Unavailable => Results.Problem(statusCode: 503, title: "Raster processing unavailable", detail: result.Error),
                _ => Results.Problem(statusCode: 500, title: "Raster processing failed", detail: result.Error)
            };
        });

        group.MapGet("/scenes/{sceneId:guid}/processings", async (Guid sceneId, ClaimsPrincipal user,
            RasterProcessingService service, CancellationToken ct) =>
            Results.Ok(await service.ListRunsAsync(GetOrganizationId(user), sceneId, ct)));

        group.MapGet("/processing-results", async (ClaimsPrincipal user, RasterProcessingService service,
            int page = 1, int pageSize = 50, Guid? fieldId = null, Guid? seasonId = null,
            Guid? managementZoneId = null, string? indexType = null, DateTime? fromUtc = null, DateTime? toUtc = null,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListResultsAsync(GetOrganizationId(user), page, pageSize, fieldId, seasonId,
                managementZoneId, indexType, fromUtc, toUtc, ct)));

        group.MapGet("/observations", async (ClaimsPrincipal user, RemoteSensingService service, int page = 1, int pageSize = 50,
            Guid? sceneId = null, Guid? fieldId = null, Guid? seasonId = null, Guid? managementZoneId = null,
            string? indexType = null, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default) =>
            Results.Ok(await service.ListObservationsAsync(GetOrganizationId(user), page, pageSize, sceneId, fieldId,
                seasonId, managementZoneId, indexType, fromUtc, toUtc, ct)));

        group.MapPost("/observations", async (CreateVegetationIndexObservationCommand command, ClaimsPrincipal user, RemoteSensingService service, CancellationToken ct) =>
        {
            var result = await service.CreateObservationAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/precision/remote-sensing/observations/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapGet("/series", async (Guid fieldId, ClaimsPrincipal user, RemoteSensingService service, Guid? seasonId = null,
            Guid? managementZoneId = null, string? indexType = null, DateTime? fromUtc = null, DateTime? toUtc = null,
            int take = 500, CancellationToken ct = default) =>
        {
            var result = await service.GetSeriesAsync(GetOrganizationId(user), fieldId, seasonId, managementZoneId,
                indexType, fromUtc, toUtc, take, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/summary", async (Guid fieldId, ClaimsPrincipal user, RemoteSensingService service, Guid? seasonId = null,
            Guid? managementZoneId = null, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default) =>
        {
            var result = await service.GetSummaryAsync(GetOrganizationId(user), fieldId, seasonId, managementZoneId, fromUtc, toUtc, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        return endpoints;
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("org_id")!);

    private static IResult ToError<T>(OperationResult<T> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["remoteSensing"] = [result.Error ?? "Invalid remote-sensing request."] }),
        OperationErrorKind.NotFound => Results.NotFound(new { message = result.Error }),
        OperationErrorKind.Conflict => Results.Conflict(new { message = result.Error }),
        _ => Results.Problem(statusCode: 500, title: "Operation failed", detail: result.Error)
    };
}
