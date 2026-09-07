using System.Security.Claims;
using AgroControl.Application.Subscriptions;
using AgroControl.Domain.Platform;

namespace AgroControl.Api.Authorization;

public sealed class ModuleAccessEndpointFilter(ModuleKey module) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var principal = context.HttpContext.User;
        if (!Guid.TryParse(principal.FindFirstValue("org_id"), out var organizationId))
            return Results.Unauthorized();

        var accessService = context.HttpContext.RequestServices.GetRequiredService<ModuleAccessService>();
        var allowed = await accessService.HasAccessAsync(organizationId, module, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Module not enabled",
                detail: $"The {module} module is not enabled for this organization.");
        }

        return await next(context);
    }
}
