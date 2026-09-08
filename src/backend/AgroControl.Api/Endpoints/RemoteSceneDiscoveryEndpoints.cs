using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class RemoteSceneDiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapRemoteSceneDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/precision/remote-sensing/discovery").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.PrecisionAgriculture));

        group.MapGet("/providers", (RemoteSceneDiscoveryService service) =>
            Results.Ok(service.GetProviders()));

        group.MapPost("/search", async (
            SearchRemoteScenesCommand command,
            ClaimsPrincipal user,
            RemoteSceneDiscoveryService service,
            CancellationToken ct) =>
        {
            var result = await service.SearchAsync(GetOrganizationId(user), command, ct);
            return ToSearchResult(result);
        });

        group.MapPost("/import", async (
            ImportRemoteSceneCommand command,
            ClaimsPrincipal user,
            RemoteSceneDiscoveryService service,
            CancellationToken ct) =>
        {
            var result = await service.ImportAsync(GetOrganizationId(user), command, ct);
            return ToImportResult(result);
        });

        return endpoints;
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("org_id")!);

    private static IResult ToSearchResult(RemoteSceneDiscoverySearchResult result) => result.Kind switch
    {
        RemoteSceneDiscoveryResultKind.Success => Results.Ok(result.Value),
        RemoteSceneDiscoveryResultKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["discovery"] = [result.Error ?? "Invalid STAC discovery request."]
        }),
        RemoteSceneDiscoveryResultKind.NotFound => Results.NotFound(new { message = result.Error }),
        RemoteSceneDiscoveryResultKind.Conflict => Results.Conflict(new { message = result.Error }),
        RemoteSceneDiscoveryResultKind.Timeout => Results.Problem(
            statusCode: 504,
            title: "STAC provider timed out",
            detail: result.Error),
        RemoteSceneDiscoveryResultKind.Unavailable => Results.Problem(
            statusCode: 503,
            title: "STAC provider unavailable",
            detail: result.Error),
        RemoteSceneDiscoveryResultKind.InvalidPayload => Results.Problem(
            statusCode: 502,
            title: "Invalid STAC provider response",
            detail: result.Error),
        _ => Results.Problem(statusCode: 500, title: "STAC discovery failed", detail: result.Error)
    };

    private static IResult ToImportResult(RemoteSceneDiscoveryImportResult result) => result.Kind switch
    {
        RemoteSceneDiscoveryResultKind.Success => Results.Created(
            $"/api/v1/precision/remote-sensing/scenes/{result.Value!.Id}",
            result.Value),
        RemoteSceneDiscoveryResultKind.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["discovery"] = [result.Error ?? "Invalid STAC import request."]
        }),
        RemoteSceneDiscoveryResultKind.NotFound => Results.NotFound(new { message = result.Error }),
        RemoteSceneDiscoveryResultKind.Conflict => Results.Conflict(new { message = result.Error }),
        RemoteSceneDiscoveryResultKind.Timeout => Results.Problem(
            statusCode: 504,
            title: "STAC provider timed out",
            detail: result.Error),
        RemoteSceneDiscoveryResultKind.Unavailable => Results.Problem(
            statusCode: 503,
            title: "STAC provider unavailable",
            detail: result.Error),
        RemoteSceneDiscoveryResultKind.InvalidPayload => Results.Problem(
            statusCode: 502,
            title: "Invalid STAC provider response",
            detail: result.Error),
        _ => Results.Problem(statusCode: 500, title: "STAC import failed", detail: result.Error)
    };
}
