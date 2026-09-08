using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class PrecisionAgricultureEndpoints
{
    public static IEndpointRouteBuilder MapPrecisionAgricultureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/precision").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.PrecisionAgriculture));

        group.MapGet("/fields", async (ClaimsPrincipal user, PrecisionAgricultureService service, Guid? farmId = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListFieldsAsync(GetOrganizationId(user), farmId, includeInactive, ct)));

        group.MapGet("/fields/{fieldId:guid}/boundary", async (Guid fieldId, ClaimsPrincipal user, PrecisionAgricultureService service, CancellationToken ct) =>
        {
            var item = await service.GetFieldAsync(GetOrganizationId(user), fieldId, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPut("/fields/{fieldId:guid}/boundary", async (Guid fieldId, GeoJsonPolygonDto polygon, ClaimsPrincipal user, PrecisionAgricultureService service, CancellationToken ct) =>
        {
            var result = await service.UpsertBoundaryAsync(GetOrganizationId(user), fieldId, polygon, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/fields/{fieldId:guid}/boundary", async (Guid fieldId, ClaimsPrincipal user, PrecisionAgricultureService service, CancellationToken ct) =>
        {
            var result = await service.DeleteBoundaryAsync(GetOrganizationId(user), fieldId, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        return endpoints;
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("org_id")!);

    private static IResult ToError<T>(OperationResult<T> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["geometry"] = [result.Error ?? "Invalid geometry."] }),
        OperationErrorKind.NotFound => Results.NotFound(new { message = result.Error }),
        OperationErrorKind.Conflict => Results.Conflict(new { message = result.Error }),
        _ => Results.Problem(statusCode: 500, title: "Operation failed", detail: result.Error)
    };
}
