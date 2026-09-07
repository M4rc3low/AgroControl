using AgroControl.Application.Platform;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IModuleCatalog, ModuleCatalog>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "AgroControl.Api",
    status = "running",
    version = "0.1.0"
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

app.Run();
