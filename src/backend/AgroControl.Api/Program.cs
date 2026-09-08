using System.Security.Claims;
using System.Text;
using AgroControl.Api.Auth;
using AgroControl.Api.Endpoints;
using AgroControl.Api.Health;
using AgroControl.Application.Finance;
using AgroControl.Application.Commercial;
using AgroControl.Application.Exporting;
using AgroControl.Application.Identity;
using AgroControl.Application.Intelligence;
using AgroControl.Application.Inventory;
using AgroControl.Application.Irrigation;
using AgroControl.Application.Machinery;
using AgroControl.Application.Market;
using AgroControl.Application.Platform;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Application.Production;
using AgroControl.Application.Subscriptions;
using AgroControl.Application.Sustainability;
using AgroControl.Domain.Platform;
using AgroControl.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddProblemDetails();
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });
builder.Services.AddSingleton<IModuleCatalog, ModuleCatalog>();
builder.Services.AddSingleton<PlanEntitlementCatalog>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ModuleAccessService>();
builder.Services.AddScoped<FarmService>();
builder.Services.AddScoped<FieldService>();
builder.Services.AddScoped<CropService>();
builder.Services.AddScoped<SeasonService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IrrigationService>();
builder.Services.AddScoped<SustainabilityService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<CommercialService>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<MachineryService>();
builder.Services.AddScoped<MarketService>();
builder.Services.AddScoped<IntelligenceService>();
builder.Services.AddScoped<PrecisionAgricultureService>();
builder.Services.AddScoped<RemoteSensingService>();
builder.Services.AddInfrastructure(builder.Configuration);

if (builder.Configuration.GetValue<bool>("Observability:Enabled"))
{
    var serviceName = builder.Configuration["Observability:ServiceName"] ?? "AgroControl.Api";

    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddMeter(
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "System.Net.Http")
            .AddOtlpExporter());
}

var jwtOptions = new JwtOptions
{
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "AgroControl",
    Audience = builder.Configuration["Jwt:Audience"] ?? "AgroControl.Web",
    Key = builder.Configuration["Jwt:Key"] ?? string.Empty,
    ExpirationMinutes = int.TryParse(builder.Configuration["Jwt:ExpirationMinutes"], out var expirationMinutes) ? expirationMinutes : 60
};

if (jwtOptions.Key.Length < 32) throw new InvalidOperationException("Jwt:Key must contain at least 32 characters.");
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer, ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)), ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations")) await app.Services.ApplyDatabaseMigrationsAsync();

app.MapGet("/", () => Results.Ok(new { service = "AgroControl.Api", status = "running", version = "0.16.0" }));
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapGet("/api/v1/platform/modules", (IModuleCatalog catalog) => Results.Ok(catalog.GetAll().Select(module => new { key = module.Key.ToString(), module.Name, status = module.Status.ToString(), module.Description })));
app.MapPost("/api/v1/auth/register", async (RegisterCommand request, AuthService authService, CancellationToken ct) =>
{
    var result = await authService.RegisterAsync(request, ct);
    return result.Succeeded ? Results.Created("/api/v1/organizations/current", result.Value) : Results.ValidationProblem(new Dictionary<string, string[]> { ["registration"] = [result.Error ?? "Registration failed."] });
});
app.MapPost("/api/v1/auth/login", async (LoginCommand request, AuthService authService, CancellationToken ct) =>
{
    var result = await authService.LoginAsync(request, ct);
    return result.Succeeded ? Results.Ok(result.Value) : Results.Problem(statusCode: 401, title: "Invalid credentials", detail: result.Error);
});

var authorized = app.MapGroup("/api/v1").RequireAuthorization();
authorized.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new
{
    userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"),
    email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email"),
    organizationId = principal.FindFirstValue("org_id"), role = principal.FindFirstValue(ClaimTypes.Role)
}));
authorized.MapGet("/organizations/current", async (ClaimsPrincipal principal, IIdentityRepository repository, CancellationToken ct) =>
{
    if (!TryGetOrganizationId(principal, out var organizationId)) return Results.Unauthorized();
    var organization = await repository.GetOrganizationAsync(organizationId, ct);
    return organization is null ? Results.NotFound() : Results.Ok(new { organization.Id, organization.Name, organization.Slug, organization.CreatedAtUtc });
});
authorized.MapGet("/platform/entitlements", async (ClaimsPrincipal principal, ModuleAccessService service, CancellationToken ct) =>
{
    if (!TryGetOrganizationId(principal, out var organizationId)) return Results.Unauthorized();
    var snapshot = await service.GetSnapshotAsync(organizationId, ct);
    return Results.Ok(new { snapshot.Plan, modules = snapshot.Modules.ToDictionary(item => item.Key.ToString(), item => item.Value) });
});
authorized.MapGet("/platform/modules/{moduleKey}/access", async (string moduleKey, ClaimsPrincipal principal, ModuleAccessService service, CancellationToken ct) =>
{
    if (!Enum.TryParse<ModuleKey>(moduleKey, true, out var module)) return Results.NotFound(new { message = "Unknown module." });
    if (!TryGetOrganizationId(principal, out var organizationId)) return Results.Unauthorized();
    return await service.HasAccessAsync(organizationId, module, ct)
        ? Results.Ok(new { module = module.ToString(), allowed = true })
        : Results.Problem(statusCode: 403, title: "Module not enabled", detail: $"The {module} module is not enabled for this organization.");
});

app.MapProductionEndpoints();
app.MapInventoryEndpoints();
app.MapFinanceEndpoints();
app.MapMachineryEndpoints();
app.MapMarketEndpoints();
app.MapIntelligenceEndpoints();
app.MapTelemetryEndpoints();
app.MapPrecisionAgricultureEndpoints();
app.MapRemoteSensingEndpoints();
app.MapIrrigationEndpoints();
app.MapSustainabilityEndpoints();
app.MapExportEndpoints();
app.MapCommercialEndpoints();
app.Run();

static bool TryGetOrganizationId(ClaimsPrincipal principal, out Guid organizationId) => Guid.TryParse(principal.FindFirstValue("org_id"), out organizationId);
