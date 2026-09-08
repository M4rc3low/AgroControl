from pathlib import Path

context_path = Path('src/backend/AgroControl.Application/RegionalOperations/OperationalScopeContext.cs')
context_path.write_text('''namespace AgroControl.Application.RegionalOperations;\n\npublic interface IOperationalScopeContext\n{\n    bool IsInitialized { get; }\n    bool IsRestricted { get; }\n    Guid OrganizationId { get; }\n    Guid UserId { get; }\n    IReadOnlyList<Guid> FarmIds { get; }\n    IReadOnlyList<Guid> RegionIds { get; }\n}\n\npublic sealed class OperationalScopeContext : IOperationalScopeContext\n{\n    private Guid[] _farmIds = [];\n    private Guid[] _regionIds = [];\n\n    public bool IsInitialized { get; private set; }\n    public bool IsRestricted { get; private set; }\n    public Guid OrganizationId { get; private set; }\n    public Guid UserId { get; private set; }\n    public IReadOnlyList<Guid> FarmIds => _farmIds;\n    public IReadOnlyList<Guid> RegionIds => _regionIds;\n\n    public void Initialize(Guid organizationId, Guid userId, FarmAccessScopeSnapshot scope)\n    {\n        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));\n        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));\n        ArgumentNullException.ThrowIfNull(scope);\n\n        OrganizationId = organizationId;\n        UserId = userId;\n        _farmIds = scope.FarmIds.Distinct().ToArray();\n        _regionIds = scope.RegionIds.Distinct().ToArray();\n        IsRestricted = !scope.AllFarms;\n        IsInitialized = true;\n    }\n}\n''')


def replace_once(path, old, new):
    p = Path(path)
    s = p.read_text()
    if old not in s:
        raise SystemExit(f'marker not found in {path}: {old[:120]!r}')
    p.write_text(s.replace(old, new, 1))


di = 'src/backend/AgroControl.Infrastructure/DependencyInjection.cs'
replace_once(
    di,
    '        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");\n        services.AddDbContext<AgroControlDbContext>(options => options.UseNpgsql(connectionString));',
    '        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");\n        services.AddScoped<OperationalScopeContext>();\n        services.AddScoped<IOperationalScopeContext>(provider => provider.GetRequiredService<OperationalScopeContext>());\n        services.AddDbContext<AgroControlDbContext>(options => options.UseNpgsql(connectionString));')

program = 'src/backend/AgroControl.Api/Program.cs'
replace_once(
    program,
    'app.UseAuthentication();\napp.UseAuthorization();',
    '''app.UseAuthentication();
app.Use(async (httpContext, next) =>
{
    if (httpContext.User.Identity?.IsAuthenticated == true &&
        TryGetOrganizationId(httpContext.User, out var organizationId) &&
        TryGetUserId(httpContext.User, out var userId))
    {
        var currentScope = httpContext.RequestServices.GetRequiredService<OperationalScopeContext>();
        var accessScope = httpContext.RequestServices.GetRequiredService<IFarmAccessScope>();
        var snapshot = await accessScope.GetEffectiveScopeAsync(organizationId, userId, httpContext.RequestAborted);
        currentScope.Initialize(organizationId, userId, snapshot);
    }

    await next();
});
app.UseAuthorization();''')
replace_once(
    program,
    'static bool TryGetOrganizationId(ClaimsPrincipal principal, out Guid organizationId) => Guid.TryParse(principal.FindFirstValue("org_id"), out organizationId);',
    'static bool TryGetOrganizationId(ClaimsPrincipal principal, out Guid organizationId) => Guid.TryParse(principal.FindFirstValue("org_id"), out organizationId);\nstatic bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out userId);')

db = Path('src/backend/AgroControl.Infrastructure/Persistence/AgroControlDbContext.cs')
s = db.read_text()
s = s.replace('using AgroControl.Domain.Modules.Crops;\n', 'using AgroControl.Application.RegionalOperations;\nusing AgroControl.Domain.Modules.Crops;\n', 1)
s = s.replace(
    'public sealed class AgroControlDbContext(DbContextOptions<AgroControlDbContext> options) : DbContext(options)\n{',
    '''public sealed class AgroControlDbContext(
    DbContextOptions<AgroControlDbContext> options,
    IOperationalScopeContext? operationalScope = null) : DbContext(options)
{
    private bool RestrictFarmScope => operationalScope is { IsInitialized: true, IsRestricted: true };
    private Guid[] ScopedFarmIds => operationalScope?.FarmIds.ToArray() ?? [];''', 1)

filters = {
    'Farm': '!RestrictFarmScope || ScopedFarmIds.Contains(x.Id)',
    'Field': '!RestrictFarmScope || ScopedFarmIds.Contains(x.FarmId)',
    'Season': '!RestrictFarmScope || Fields.Any(field => field.Id == x.FieldId && ScopedFarmIds.Contains(field.FarmId))',
    'Warehouse': '!RestrictFarmScope || x.FarmId == null || ScopedFarmIds.Contains(x.FarmId.Value)',
    'StockMovement': '!RestrictFarmScope || (x.FarmId != null && ScopedFarmIds.Contains(x.FarmId.Value)) || (x.FarmId == null && Warehouses.Any(warehouse => warehouse.Id == x.WarehouseId && (warehouse.FarmId == null || ScopedFarmIds.Contains(warehouse.FarmId.Value))))',
    'FinancialTransaction': '!RestrictFarmScope || x.FarmId == null || ScopedFarmIds.Contains(x.FarmId.Value)',
    'Machine': '!RestrictFarmScope || x.FarmId == null || ScopedFarmIds.Contains(x.FarmId.Value)',
    'HourMeterReading': '!RestrictFarmScope || Machines.Any(machine => machine.Id == x.MachineId && (machine.FarmId == null || ScopedFarmIds.Contains(machine.FarmId.Value)))',
    'Fueling': '!RestrictFarmScope || Machines.Any(machine => machine.Id == x.MachineId && (machine.FarmId == null || ScopedFarmIds.Contains(machine.FarmId.Value)))',
    'MaintenanceRecord': '!RestrictFarmScope || Machines.Any(machine => machine.Id == x.MachineId && (machine.FarmId == null || ScopedFarmIds.Contains(machine.FarmId.Value)))',
    'IrrigationZone': '!RestrictFarmScope || Fields.Any(field => field.Id == x.FieldId && ScopedFarmIds.Contains(field.FarmId))',
    'IrrigationApplication': '!RestrictFarmScope || Fields.Any(field => field.Id == x.FieldId && ScopedFarmIds.Contains(field.FarmId))',
    'EmissionActivity': '!RestrictFarmScope || x.FarmId == null || ScopedFarmIds.Contains(x.FarmId.Value)',
    'ExportOrder': '!RestrictFarmScope || x.FarmId == null || ScopedFarmIds.Contains(x.FarmId.Value)',
    'ExportDocument': '!RestrictFarmScope || ExportOrders.Any(order => order.Id == x.OrderId)',
    'ExportCost': '!RestrictFarmScope || ExportOrders.Any(order => order.Id == x.OrderId)',
    'ExportOrderStatusEvent': '!RestrictFarmScope || ExportOrders.Any(order => order.Id == x.OrderId)',
    'CommercialOpportunity': '!RestrictFarmScope || x.FarmId == null || ScopedFarmIds.Contains(x.FarmId.Value)',
    'OpportunityStageEvent': '!RestrictFarmScope || CommercialOpportunities.Any(opportunity => opportunity.Id == x.OpportunityId)',
}

for entity, expr in filters.items():
    marker = f'modelBuilder.Entity<{entity}>(entity =>\n        {{'
    if marker not in s:
        raise SystemExit(f'entity block not found: {entity}')
    s = s.replace(marker, marker + f'\n            entity.HasQueryFilter(x => {expr});', 1)

db.write_text(s)
