using System.Security.Claims;
using AgroControl.Api.Authorization;
using AgroControl.Application.Commercial;
using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Commercial;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Endpoints;

public static class CommercialEndpoints
{
    public static IEndpointRouteBuilder MapCommercialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/commercial").RequireAuthorization();
        group.AddEndpointFilter(new ModuleAccessEndpointFilter(ModuleKey.Commercial));

        group.MapGet("/customers", async (ClaimsPrincipal user, CommercialService service, int page = 1, int pageSize = 20,
            string? search = null, CustomerStatus? status = null, CancellationToken ct = default) =>
            Results.Ok(await service.ListCustomersAsync(GetOrganizationId(user), page, pageSize, search, status, ct)));

        group.MapGet("/customers/{id:guid}", async (Guid id, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var item = await service.GetCustomerAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/customers", async (CreateCommercialCustomerCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.CreateCustomerAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/commercial/customers/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/customers/{id:guid}", async (Guid id, UpdateCommercialCustomerCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.UpdateCustomerAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/customers/{id:guid}/contacts", async (Guid id, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.ListContactsAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/customers/{id:guid}/contacts", async (Guid id, CreateCommercialContactCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.CreateContactAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/commercial/customers/{id}/contacts/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/contacts/{id:guid}", async (Guid id, UpdateCommercialContactCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.UpdateContactAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapDelete("/contacts/{id:guid}", async (Guid id, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.DeactivateContactAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/opportunities", async (ClaimsPrincipal user, CommercialService service, int page = 1, int pageSize = 20,
            string? search = null, Guid? customerId = null, OpportunityStage? stage = null, string? ownerName = null,
            string? currency = null, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default) =>
            Results.Ok(await service.ListOpportunitiesAsync(GetOrganizationId(user), page, pageSize, search, customerId,
                stage, ownerName, currency, from, to, ct)));

        group.MapGet("/opportunities/{id:guid}", async (Guid id, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var item = await service.GetOpportunityAsync(GetOrganizationId(user), id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/opportunities", async (CreateCommercialOpportunityCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.CreateOpportunityAsync(GetOrganizationId(user), command, ct);
            return result.Succeeded ? Results.Created($"/api/v1/commercial/opportunities/{result.Value!.Id}", result.Value) : ToError(result);
        });

        group.MapPut("/opportunities/{id:guid}", async (Guid id, UpdateCommercialOpportunityCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.UpdateOpportunityAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapPost("/opportunities/{id:guid}/stage", async (Guid id, TransitionOpportunityStageCommand command, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.TransitionStageAsync(GetOrganizationId(user), id, command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/opportunities/{id:guid}/timeline", async (Guid id, ClaimsPrincipal user, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.GetTimelineAsync(GetOrganizationId(user), id, ct);
            return result.Succeeded ? Results.Ok(result.Value) : ToError(result);
        });

        group.MapGet("/summary", async (ClaimsPrincipal user, CommercialService service, DateOnly? from = null,
            DateOnly? to = null, string? ownerName = null, string? currency = null, CancellationToken ct = default) =>
            Results.Ok(await service.GetSummaryAsync(GetOrganizationId(user), from, to, ownerName, currency, ct)));

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
