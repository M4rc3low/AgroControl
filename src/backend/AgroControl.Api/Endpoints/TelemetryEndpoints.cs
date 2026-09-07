using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Telemetry;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/telemetry").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Telemetry));

        group.MapGet("/devices", async (ClaimsPrincipal user, ITelemetryClient client, int limit, CancellationToken ct) =>
            await Execute(() => client.ListDevicesAsync(OrganizationId(user), limit == 0 ? 100 : limit, ct)));

        group.MapPost("/devices", async (ClaimsPrincipal user, CreateTelemetryDeviceCommand command, ITelemetryClient client, CancellationToken ct) =>
            await Execute(() => client.CreateDeviceAsync(OrganizationId(user), command, ct), created: true));

        group.MapGet("/devices/{deviceId:guid}", async (Guid deviceId, ClaimsPrincipal user, ITelemetryClient client, CancellationToken ct) =>
            await Execute(() => client.GetDeviceAsync(OrganizationId(user), deviceId, ct)));

        group.MapPatch("/devices/{deviceId:guid}/status", async (Guid deviceId, ClaimsPrincipal user, UpdateTelemetryStatus command, ITelemetryClient client, CancellationToken ct) =>
            await Execute(() => client.UpdateStatusAsync(OrganizationId(user), deviceId, command.Status, ct)));

        group.MapPost("/devices/{deviceId:guid}/events", async (Guid deviceId, ClaimsPrincipal user, CreateTelemetryEventCommand command, ITelemetryClient client, CancellationToken ct) =>
            await Execute(() => client.IngestAsync(OrganizationId(user), deviceId, command, ct), created: true));

        group.MapGet("/devices/{deviceId:guid}/latest", async (Guid deviceId, ClaimsPrincipal user, ITelemetryClient client, string? metric, CancellationToken ct) =>
            await Execute(() => client.GetLatestAsync(OrganizationId(user), deviceId, metric, ct)));

        group.MapGet("/devices/{deviceId:guid}/events", async (Guid deviceId, ClaimsPrincipal user, ITelemetryClient client, string? metric, DateTimeOffset? from, DateTimeOffset? to, int limit, CancellationToken ct) =>
            await Execute(() => client.GetHistoryAsync(OrganizationId(user), deviceId, metric, from, to, limit == 0 ? 100 : limit, ct)));

        return endpoints;
    }

    private static Guid OrganizationId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("org_id")!);

    private static async Task<IResult> Execute<T>(Func<Task<T>> action, bool created = false)
    {
        try
        {
            var value = await action();
            return created ? Results.Json(value, statusCode: StatusCodes.Status201Created) : Results.Ok(value);
        }
        catch (TelemetryClientException ex)
        {
            return ex.Kind switch
            {
                TelemetryClientErrorKind.NotFound => Results.NotFound(new { message = ex.Message }),
                TelemetryClientErrorKind.Validation => Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status422UnprocessableEntity),
                TelemetryClientErrorKind.Conflict => Results.Conflict(new { message = ex.Message }),
                TelemetryClientErrorKind.Timeout => Results.Problem(statusCode: StatusCodes.Status504GatewayTimeout, title: "Telemetry timeout", detail: ex.Message),
                _ => Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Telemetry unavailable", detail: ex.Message)
            };
        }
    }

    public sealed record UpdateTelemetryStatus(string Status);
}
