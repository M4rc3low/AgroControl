using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Intelligence;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class IntelligenceEndpoints
{
    public static IEndpointRouteBuilder MapIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/intelligence").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Intelligence));

        group.MapPost("/seasons/{seasonId:guid}/yield-prediction", async (
            Guid seasonId,
            ClaimsPrincipal user,
            IntelligenceService service,
            CancellationToken cancellationToken) =>
        {
            var organizationId = Guid.Parse(user.FindFirstValue("org_id")!);
            var result = await service.PredictSeasonYieldAsync(
                organizationId,
                seasonId,
                cancellationToken);

            return result.Kind switch
            {
                PredictionResultKind.Success => Results.Ok(result.Value),
                PredictionResultKind.NotFound => Results.NotFound(new { message = result.Error }),
                PredictionResultKind.InsufficientData => Results.Json(
                    result.Value,
                    statusCode: StatusCodes.Status422UnprocessableEntity),
                PredictionResultKind.Validation => Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["prediction"] = [result.Error ?? "Invalid prediction request."]
                    }),
                PredictionResultKind.Timeout => Results.Problem(
                    statusCode: StatusCodes.Status504GatewayTimeout,
                    title: "Intelligence timeout",
                    detail: result.Error),
                _ => Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Intelligence unavailable",
                    detail: result.Error)
            };
        });

        return endpoints;
    }
}
