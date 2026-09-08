using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Sustainability;
using AgroControl.Domain.Modules.Sustainability;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class SustainabilityEndpoints
{
    public static IEndpointRouteBuilder MapSustainabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/sustainability").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Sustainability));

        group.MapGet("/emission-factors", async (
            ClaimsPrincipal user, SustainabilityService service, int page = 1, int pageSize = 20,
            string? search = null, EmissionSourceCategory? category = null, bool includeInactive = false,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListFactorsAsync(GetOrganizationId(user), page, pageSize, search, category, includeInactive, ct)));

        group.MapGet("/emission-factors/{id:guid}", async (
            Guid id, ClaimsPrincipal user, SustainabilityService service, CancellationToken ct) =>
        {
            var item = await service.GetFactorAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/emission-factors", async (
            CreateEmissionFactorCommand command, ClaimsPrincipal user, SustainabilityService service, CancellationToken ct) =>
        {
            var result = await service.CreateFactorAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded
                ? Results.Created($"/api/v1/sustainability/emission-factors/{result.Value!.Id}", result.Value)
                : ToError(result);
        });

        group.MapPut("/emission-factors/{id:guid}", async (
            Guid id, UpdateEmissionFactorCommand command, ClaimsPrincipal user, SustainabilityService service, CancellationToken ct) =>
        {
            var result = await service.UpdateFactorAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/emission-factors/{id:guid}", async (
            Guid id, ClaimsPrincipal user, SustainabilityService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateFactorAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/activities", async (
            ClaimsPrincipal user, SustainabilityService service, int page = 1, int pageSize = 20,
            DateOnly? from = null, DateOnly? to = null, EmissionSourceCategory? category = null,
            Guid? farmId = null, Guid? fieldId = null, Guid? seasonId = null, string? sourceModule = null,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListActivitiesAsync(
                GetOrganizationId(user), page, pageSize, from, to, category, farmId, fieldId, seasonId, sourceModule, ct)));

        group.MapPost("/activities", async (
            CreateEmissionActivityCommand command, ClaimsPrincipal user, SustainabilityService service, CancellationToken ct) =>
        {
            var result = await service.CreateActivityAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded
                ? Results.Created($"/api/v1/sustainability/activities/{result.Value!.Id}", result.Value)
                : ToError(result);
        });

        group.MapGet("/summary", async (
            ClaimsPrincipal user, SustainabilityService service, DateOnly? from = null, DateOnly? to = null,
            Guid? farmId = null, Guid? fieldId = null, Guid? seasonId = null, CancellationToken ct = default) =>
        {
            var result = await service.GetSummaryAsync(GetOrganizationId(user), from, to, farmId, fieldId, seasonId, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/seasons/{seasonId:guid}/summary", async (
            Guid seasonId, ClaimsPrincipal user, SustainabilityService service, CancellationToken ct) =>
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
