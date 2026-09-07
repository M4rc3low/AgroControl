using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgroControl.Infrastructure.Persistence;

public sealed class AgroControlDbContextFactory : IDesignTimeDbContextFactory<AgroControlDbContext>
{
    public AgroControlDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=agrocontrol;Username=agrocontrol;Password=agrocontrol_dev";

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AgroControlDbContext(options);
    }
}
