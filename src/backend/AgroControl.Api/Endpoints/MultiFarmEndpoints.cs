using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class MultiFarmEndpoints
{
    public static IEndpointRouteBuilder MapMultiFarmEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/operations").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Farms));

        group.MapGet("/regions", async (
            ClaimsPrincipal user,
            MultiFarmService service,
            int page = 1,
            int pageSize = 50,
            string? search = null,
            bool includeInactive = false,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListRegionsAsync(GetOrganizationId(user), page, pageSize, search, includeInactive, ct)));

        group.MapPost("/regions", async (
            CreateOperationalRegionCommand command,
            ClaimsPrincipal user,
            MultiFarmService service,
            CancellationToken ct) =>
        {
            if (!CanManageScopes(user)) return Results.Forbid();
            var result = await service.CreateRegionAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded
                ? Results.Created($"/api/v1/operations/regions/{result.Value!.Id}", result.Value)
                : ToError(result);
        });

        group.MapPut("/regions/{regionId:guid}", async (
            Guid regionId,
            UpdateOperationalRegionCommand command,
            ClaimsPrincipal user,
            MultiFarmService service,
            CancellationToken ct) =>
        {
            if (!CanManageScopes(user)) return Results.Forbid();
            var result = await service.UpdateRegionAsync(GetOrganizationId(user), regionId, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/regions/{regionId:guid}", async (
            Guid regionId,
            ClaimsPrincipal user,
            MultiFarmService service,
            CancellationToken ct) =>
        {
            if (!CanManageScopes(user)) return Results.Forbid();
            var result = await service.DeactivateRegionAsync(GetOrganizationId(user), regionId, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        group.MapGet("/farm-access/me", async (
            ClaimsPrincipal user,
            MultiFarmService service,
            CancellationToken ct) =>
            Results.Ok(await service.GetEffectiveScopeAsync(GetOrganizationId(user), GetUserId(user), ct)));

        group.MapGet("/farm-access/users/{userId:guid}", async (
            Guid userId,
            ClaimsPrincipal user,
            MultiFarmService service,
            bool includeInactive = false,
            CancellationToken ct = default) =>
        {
            if (!CanManageScopes(user)) return Results.Forbid();
            var items = await service.ListAssignmentsAsync(GetOrganizationId(user), userId, includeInactive, ct);
            return Results.Ok(items);
        });

        group.MapPost("/farm-access", async (
            GrantFarmAccessCommand command,
            ClaimsPrincipal user,
            MultiFarmService service,
            CancellationToken ct) =>
        {
            if (!CanManageScopes(user)) return Results.Forbid();
            var result = await service.GrantAccessAsync(GetOrganizationId(user), GetUserId(user), command, ct);
            return result.Succeeded
                ? Results.Created($"/api/v1/operations/farm-access/{result.Value!.Id}", result.Value)
                : ToError(result);
        });

        group.MapDelete("/farm-access/{assignmentId:guid}", async (
            Guid assignmentId,
            ClaimsPrincipal user,
            MultiFarmService service,
            CancellationToken ct) =>
        {
            if (!CanManageScopes(user)) return Results.Forbid();
            var result = await service.RevokeAccessAsync(GetOrganizationId(user), GetUserId(user), assignmentId, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });

        return endpoints;
    }

    private static bool CanManageScopes(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<OrganizationRole>(role, true, out var parsed) && parsed is OrganizationRole.Owner or OrganizationRole.Admin;
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("org_id")!);
    private static Guid GetUserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub")!);

    private static IResult ToError<T>(OperationResult<T> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error ?? "Invalid request."] }),
        OperationErrorKind.NotFound => Results.NotFound(new { message = result.Error }),
        OperationErrorKind.Conflict => Results.Conflict(new { message = result.Error }),
        _ => Results.Problem(statusCode: 500, title: "Operation failed", detail: result.Error)
    };
}
