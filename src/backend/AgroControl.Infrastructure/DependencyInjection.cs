using AgroControl.Application.Common;
using AgroControl.Application.Commercial;
using AgroControl.Application.Finance;
using AgroControl.Application.Exporting;
using AgroControl.Application.Identity;
using AgroControl.Application.Intelligence;
using AgroControl.Application.Inventory;
using AgroControl.Application.Irrigation;
using AgroControl.Application.Machinery;
using AgroControl.Application.Market;
using AgroControl.Application.PrecisionAgriculture;
using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Application.Subscriptions;
using AgroControl.Application.Sustainability;
using AgroControl.Application.Telemetry;
using AgroControl.Infrastructure.Intelligence;
using AgroControl.Infrastructure.Persistence;
using AgroControl.Infrastructure.Security;
using AgroControl.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        services.AddScoped<OperationalScopeContext>();
        services.AddScoped<IOperationalScopeContext>(provider => provider.GetRequiredService<OperationalScopeContext>());
        services.AddDbContext<AgroControlDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IProductionRepository, ProductionRepository>();
        services.AddScoped<IMultiFarmRepository, MultiFarmRepository>();
        services.AddScoped<IFarmAccessScope, FarmAccessScopeService>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IIrrigationRepository, IrrigationRepository>();
        services.AddScoped<ISustainabilityRepository, SustainabilityRepository>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<ICommercialRepository, CommercialRepository>();
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IMachineryRepository, MachineryRepository>();
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<IPrecisionAgricultureRepository, PrecisionAgricultureRepository>();
        services.AddScoped<IRemoteSensingRepository, RemoteSensingRepository>();
        services.AddScoped<IRasterProcessingRepository, RasterProcessingRepository>();
        services.AddScoped<IIntelligenceDataSource, IntelligenceDataSource>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        var intelligenceBaseUrl = configuration["Intelligence:BaseUrl"] ?? "http://localhost:8090";
        var intelligenceTimeout = int.TryParse(configuration["Intelligence:TimeoutSeconds"], out var configuredIntelligence) ? Math.Clamp(configuredIntelligence, 1, 300) : 30;
        services.AddHttpClient<IIntelligenceClient, IntelligenceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(intelligenceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(intelligenceTimeout);
        });

        var telemetryBaseUrl = configuration["Telemetry:BaseUrl"] ?? "http://localhost:8100";
        var telemetryTimeout = int.TryParse(configuration["Telemetry:TimeoutSeconds"], out var configuredTelemetry) ? Math.Clamp(configuredTelemetry, 1, 60) : 5;
        if ((configuration["Telemetry:InternalApiKey"] ?? string.Empty).Length < 32)
            throw new InvalidOperationException("Telemetry:InternalApiKey must contain at least 32 characters.");
        services.AddHttpClient<ITelemetryClient, TelemetryHttpClient>(client =>
        {
            client.BaseAddress = new Uri(telemetryBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(telemetryTimeout);
        });
        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AgroControlDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
