using System.Security.Claims;
using System.Text;
using AgroControl.Api.Auth;
using AgroControl.Api.Endpoints;
using AgroControl.Application.Identity;
using AgroControl.Application.Platform;
using AgroControl.Application.Production;
using AgroControl.Application.Subscriptions;
using AgroControl.Domain.Platform;
using AgroControl.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IModuleCatalog, ModuleCatalog>();
builder.Services.AddSingleton<PlanEntitlementCatalog>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ModuleAccessService>();
builder.Services.AddScoped<FarmService>();
builder.Services.AddScoped<FieldService>();
builder.Services.AddScoped<CropService>();
builder.Services.AddScoped<SeasonService>();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = new JwtOptions
{
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "AgroControl",
    Audience = builder.Configuration["Jwt:Audience"] ?? "AgroControl.Web",
    Key = builder.Configuration["Jwt:Key"] ?? string.Empty,
    ExpirationMinutes = int.TryParse(builder.Configuration["Jwt:ExpirationMinutes"], out var expirationMinutes)
        ? expirationMinutes
        : 60
};

if (jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 characters.");

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
    await app.Services.ApplyDatabaseMigrationsAsync();

app.MapGet("/", () => Results.Ok(new
{
    service = "AgroControl.Api",
    status = "running",
    version = "0.3.0"
}));

app.MapHealthChecks("/health");

app.MapGet("/api/v1/platform/modules", (IModuleCatalog catalog) =>
{
    var modules = catalog.GetAll().Select(module => new
    {
        key = module.Key.ToString(),
        module.Name,
        status = module.Status.ToString(),
        module.Description
    });

    return Results.Ok(modules);
});

app.MapPost("/api/v1/auth/register", async (
    RegisterCommand request,
    AuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.RegisterAsync(request, cancellationToken);
    return result.Succeeded
        ? Results.Created("/api/v1/organizations/current", result.Value)
        : Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["registration"] = [result.Error ?? "Registration failed."]
        });
});

app.MapPost("/api/v1/auth/login", async (
    LoginCommand request,
    AuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.LoginAsync(request, cancellationToken);
    return result.Succeeded
        ? Results.Ok(result.Value)
        : Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials", detail: result.Error);
});

var authorized = app.MapGroup("/api/v1").RequireAuthorization();

authorized.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new
{
    userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"),
    email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email"),
    organizationId = principal.FindFirstValue("org_id"),
    role = principal.FindFirstValue(ClaimTypes.Role)
}));

authorized.MapGet("/organizations/current", async (
    ClaimsPrincipal principal,
    IIdentityRepository identityRepository,
    CancellationToken cancellationToken) =>
{
    if (!TryGetOrganizationId(principal, out var organizationId))
        return Results.Unauthorized();

    var organization = await identityRepository.GetOrganizationAsync(organizationId, cancellationToken);
    return organization is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.CreatedAtUtc
        });
});

authorized.MapGet("/platform/entitlements", async (
    ClaimsPrincipal principal,
    ModuleAccessService moduleAccessService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetOrganizationId(principal, out var organizationId))
        return Results.Unauthorized();

    var snapshot = await moduleAccessService.GetSnapshotAsync(organizationId, cancellationToken);
    return Results.Ok(new
    {
        snapshot.Plan,
        modules = snapshot.Modules.ToDictionary(item => item.Key.ToString(), item => item.Value)
    });
});

authorized.MapGet("/platform/modules/{moduleKey}/access", async (
    string moduleKey,
    ClaimsPrincipal principal,
    ModuleAccessService moduleAccessService,
    CancellationToken cancellationToken) =>
{
    if (!Enum.TryParse<ModuleKey>(moduleKey, true, out var module))
        return Results.NotFound(new { message = "Unknown module." });

    if (!TryGetOrganizationId(principal, out var organizationId))
        return Results.Unauthorized();

    var allowed = await moduleAccessService.HasAccessAsync(organizationId, module, cancellationToken);
    return allowed
        ? Results.Ok(new { module = module.ToString(), allowed = true })
        : Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Module not enabled",
            detail: $"The {module} module is not enabled for this organization.");
});

app.MapProductionEndpoints();

app.Run();

static bool TryGetOrganizationId(ClaimsPrincipal principal, out Guid organizationId) =>
    Guid.TryParse(principal.FindFirstValue("org_id"), out organizationId);
