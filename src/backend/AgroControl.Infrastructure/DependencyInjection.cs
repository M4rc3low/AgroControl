using AgroControl.Application.Common;
using AgroControl.Application.Finance;
using AgroControl.Application.Identity;
using AgroControl.Application.Inventory;
using AgroControl.Application.Machinery;
using AgroControl.Application.Market;
using AgroControl.Application.Production;
using AgroControl.Application.Subscriptions;
using AgroControl.Infrastructure.Persistence;
using AgroControl.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AgroControlDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IProductionRepository, ProductionRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IMachineryRepository, MachineryRepository>();
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AgroControlDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
