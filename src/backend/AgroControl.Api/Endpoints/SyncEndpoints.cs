using System.Diagnostics;
using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Common;
using AgroControl.Application.Sync;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/sync").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Farms));

        group.MapGet("/status", async (
            Guid farmId,
            ClaimsPrincipal user,
            OfflineSyncService service,
            CancellationToken ct) =>
        {
            var result = await service.GetStatusAsync(
                GetOrganizationId(user),
                GetUserId(user),
                farmId,
                ct);

            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/bootstrap", async (
            Guid farmId,
            ClaimsPrincipal user,
            OfflineSyncService service,
            CancellationToken ct) =>
        {
            var result = await service.BootstrapAsync(
                GetOrganizationId(user),
                GetUserId(user),
                farmId,
                ct);

            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/pull", async (
            Guid farmId,
            string cursor,
            ClaimsPrincipal user,
            OfflineSyncService service,
            int take = 200,
            CancellationToken ct = default) =>
        {
            var started = Stopwatch.GetTimestamp();
            var result = await service.PullAsync(
                GetOrganizationId(user),
                GetUserId(user),
                farmId,
                cursor,
                take,
                ct);

            if (!result.Succeeded &&
                result.ErrorKind == OperationErrorKind.Validation &&
                result.Error?.Contains("cursor", StringComparison.OrdinalIgnoreCase) == true)
            {
                OfflineSyncTelemetry.RecordInvalidCursor();
            }
            if (!result.Succeeded && result.ErrorKind == OperationErrorKind.Forbidden)
                OfflineSyncTelemetry.RecordAccessRevocation("pull");

            OfflineSyncTelemetry.RecordBatch(
                "pull",
                result.Succeeded ? "completed" : result.ErrorKind?.ToString() ?? "other",
                started,
                result.Value?.Changes.Count ?? 0);

            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/push", async (
            OfflinePushRequestDto request,
            ClaimsPrincipal user,
            OfflinePushService service,
            CancellationToken ct) =>
        {
            var started = Stopwatch.GetTimestamp();
            var result = await service.PushAsync(
                GetOrganizationId(user),
                GetUserId(user),
                request,
                ct);

            if (result.Succeeded)
            {
                foreach (var operation in result.Value!.Results)
                {
                    OfflineSyncTelemetry.RecordOperation(operation.EntityKind, operation.Status, operation.Replayed);
                    if (operation.Status == "RetryableError") OfflineSyncTelemetry.RecordRetry();
                    if (operation.Status == "Forbidden" && operation.ErrorCode == "FarmAccessRevoked")
                        OfflineSyncTelemetry.RecordAccessRevocation("push");
                }
            }
            else if (result.ErrorKind == OperationErrorKind.Forbidden)
            {
                OfflineSyncTelemetry.RecordAccessRevocation("push");
            }

            OfflineSyncTelemetry.RecordBatch(
                "push",
                result.Succeeded ? "completed" : result.ErrorKind?.ToString() ?? "other",
                started,
                request?.Operations?.Count ?? 0);

            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        return endpoints;
    }

    private static Guid GetOrganizationId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("org_id")!);

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub")!);

    private static IResult ToError<T>(OperationResult<T> result) => result.ErrorKind switch
    {
        OperationErrorKind.Validation => Results.ValidationProblem(
            new Dictionary<string, string[]> { ["request"] = [result.Error ?? "Invalid request."] }),
        OperationErrorKind.NotFound => Results.NotFound(new { message = result.Error }),
        OperationErrorKind.Conflict => Results.Conflict(new { message = result.Error }),
        OperationErrorKind.Forbidden => Results.Problem(
            statusCode: 403,
            title: "Offline synchronization access denied",
            detail: result.Error),
        _ => Results.Problem(statusCode: 500, title: "Sync operation failed", detail: result.Error)
    };
}
