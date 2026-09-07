using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class ProductionPersistenceTests
{
    [Fact]
    public async Task Production_repository_isolates_farms_by_organization()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var organizationA = Organization.Create("Grupo A", $"grupo-a-{Guid.NewGuid():N}", now);
        var organizationB = Organization.Create("Grupo B", $"grupo-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(organizationA, organizationB);
        dbContext.Farms.Add(Farm.Create(organizationA.Id, "Fazenda A", 100m, "Rio Verde", "GO", now));
        dbContext.Farms.Add(Farm.Create(organizationB.Id, "Fazenda B", 200m, "Sorriso", "MT", now));
        await dbContext.SaveChangesAsync();

        var repository = new ProductionRepository(dbContext);
        var (items, totalCount) = await repository.ListFarmsAsync(
            organizationA.Id,
            skip: 0,
            take: 20,
            search: null,
            includeInactive: false);

        Assert.Equal(1, totalCount);
        var farm = Assert.Single(items);
        Assert.Equal("Fazenda A", farm.Name);
        Assert.Equal(organizationA.Id, farm.OrganizationId);
    }
}
