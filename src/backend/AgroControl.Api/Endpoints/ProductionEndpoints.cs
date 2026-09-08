using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Seasons;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class ProductionEndpoints
{
    public static IEndpointRouteBuilder MapProductionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapFarmEndpoints(endpoints);
        MapFieldEndpoints(endpoints);
        MapCropEndpoints(endpoints);
        MapSeasonEndpoints(endpoints);
        return endpoints;
    }

    private static void MapFarmEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/farms").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Farms));

        group.MapGet("/", async (
            ClaimsPrincipal user,
            FarmService service,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            bool includeInactive = false,
            Guid? regionId = null,
            string? stateCode = null,
            CancellationToken ct = default) =>
            Results.Ok(await service.ListAsync(
                GetOrganizationId(user), GetUserId(user), page, pageSize, search, includeInactive, regionId, stateCode, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, FarmService service, CancellationToken ct) =>
        {
            var item = await service.GetAsync(GetOrganizationId(user), GetUserId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async (CreateFarmCommand command, ClaimsPrincipal user, FarmService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(GetOrganizationId(user), GetUserId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/farms/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateFarmCommand command, ClaimsPrincipal user, FarmService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(GetOrganizationId(user), GetUserId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, FarmService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateAsync(GetOrganizationId(user), GetUserId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });
    }

    private static void MapFieldEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/fields").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Fields));

        group.MapGet("/", async (ClaimsPrincipal user, FieldService service, int page = 1, int pageSize = 20, Guid? farmId = null, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListAsync(GetOrganizationId(user), GetUserId(user), page, pageSize, farmId, search, includeInactive, ct)));
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, FieldService service, CancellationToken ct) =>
        {
            var item = await service.GetAsync(GetOrganizationId(user), GetUserId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        group.MapPost("/", async (CreateFieldCommand command, ClaimsPrincipal user, FieldService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(GetOrganizationId(user), GetUserId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/fields/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/{id:guid}", async (Guid id, UpdateFieldCommand command, ClaimsPrincipal user, FieldService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(GetOrganizationId(user), GetUserId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, FieldService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateAsync(GetOrganizationId(user), GetUserId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });
    }

    private static void MapCropEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/crops").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Crops));

        group.MapGet("/", async (ClaimsPrincipal user, CropService service, int page = 1, int pageSize = 20, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListAsync(GetOrganizationId(user), page, pageSize, search, includeInactive, ct)));
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, CropService service, CancellationToken ct) =>
        {
            var item = await service.GetAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        group.MapPost("/", async (CreateCropCommand command, ClaimsPrincipal user, CropService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/crops/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/{id:guid}", async (Guid id, UpdateCropCommand command, ClaimsPrincipal user, CropService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, CropService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });
    }

    private static void MapSeasonEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/seasons").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Seasons));

        group.MapGet("/", async (ClaimsPrincipal user, SeasonService service, int page = 1, int pageSize = 20, Guid? fieldId = null, SeasonStatus? status = null, string? search = null, bool includeInactive = false, CancellationToken ct = default) =>
            Results.Ok(await service.ListAsync(GetOrganizationId(user), GetUserId(user), page, pageSize, fieldId, status, search, includeInactive, ct)));
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, SeasonService service, CancellationToken ct) =>
        {
            var item = await service.GetAsync(GetOrganizationId(user), GetUserId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });
        group.MapPost("/", async (CreateSeasonCommand command, ClaimsPrincipal user, SeasonService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(GetOrganizationId(user), GetUserId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/seasons/{result.Value!.Id}", result.Value) : ToError(result);
        });
        group.MapPut("/{id:guid}", async (Guid id, UpdateSeasonCommand command, ClaimsPrincipal user, SeasonService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(GetOrganizationId(user), GetUserId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, SeasonService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateAsync(GetOrganizationId(user), GetUserId(user), id, ct);
            return result.Succeeded ? Results.NoContent() : ToError(result);
        });
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("org_id")!);
    private static Guid GetUserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub")!);

    private static IResult ToError<T>(OperationResult<T> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error ?? "Invalid request."] }),
        OperationErrorKind.NotFound => Results.NotFound(new { message = result.Error }),
        OperationErrorKind.Conflict => Results.Conflict(new { message = result.Error }),
        OperationErrorKind.Forbidden => Results.Problem(statusCode: 403, title: "Farm access denied", detail: result.Error),
        _ => Results.Problem(statusCode: 500, title: "Operation failed", detail: result.Error)
    };
}
