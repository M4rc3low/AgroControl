using AgroControl.Application.Production;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Operations;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class MultiFarmPersistenceTests
{
    [Fact]
    public async Task Regional_scope_filters_farms_inside_the_same_organization()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var orgA = Organization.Create("Grupo Multi A", $"multi-a-{Guid.NewGuid():N}", now);
        var orgB = Organization.Create("Grupo Multi B", $"multi-b-{Guid.NewGuid():N}", now);
        var owner = User.Create($"owner-{Guid.NewGuid():N}@example.com", "Owner", "hash", now);
        var manager = User.Create($"manager-{Guid.NewGuid():N}@example.com", "Manager", "hash", now);
        var otherUser = User.Create($"other-{Guid.NewGuid():N}@example.com", "Other", "hash", now);
        var ownerMembership = OrganizationMembership.Create(orgA.Id, owner.Id, OrganizationRole.Owner, now);
        var managerMembership = OrganizationMembership.Create(orgA.Id, manager.Id, OrganizationRole.Manager, now);
        var otherMembership = OrganizationMembership.Create(orgB.Id, otherUser.Id, OrganizationRole.Owner, now);
        var centroOeste = OperationalRegion.Create(orgA.Id, "Centro-Oeste", "CO", null, now);
        var farmGo = Farm.Create(orgA.Id, "Fazenda GO", 500m, "Rio Verde", "GO", now, centroOeste.Id, "BR", "GO", null, null, -17.79m, -50.92m, "America/Sao_Paulo");
        var farmPr = Farm.Create(orgA.Id, "Fazenda PR", 300m, "Cascavel", "PR", now, null, "BR", "PR", null, null, -24.95m, -53.45m, "America/Sao_Paulo");
        var farmOtherOrg = Farm.Create(orgB.Id, "Fazenda B", 900m, "Sorriso", "MT", now, null, "BR", "MT", null, null, -12.54m, -55.72m, "America/Cuiaba");
        var ownerAll = FarmAccessAssignment.CreateAllFarms(orgA.Id, owner.Id, owner.Id, now);
        var managerRegion = FarmAccessAssignment.Create(orgA.Id, manager.Id, FarmAccessScopeType.Region, centroOeste.Id, owner.Id, now);
        var otherAll = FarmAccessAssignment.CreateAllFarms(orgB.Id, otherUser.Id, otherUser.Id, now);

        dbContext.Organizations.AddRange(orgA, orgB);
        dbContext.Users.AddRange(owner, manager, otherUser);
        dbContext.OrganizationMemberships.AddRange(ownerMembership, managerMembership, otherMembership);
        dbContext.OperationalRegions.Add(centroOeste);
        dbContext.Farms.AddRange(farmGo, farmPr, farmOtherOrg);
        dbContext.FarmAccessAssignments.AddRange(ownerAll, managerRegion, otherAll);
        await dbContext.SaveChangesAsync();

        var multiFarmRepository = new MultiFarmRepository(dbContext);
        var accessScope = new FarmAccessScopeService(multiFarmRepository);
        var productionRepository = new ProductionRepository(dbContext);
        var farmService = new FarmService(productionRepository, multiFarmRepository, accessScope, new UnitOfWork(dbContext));

        var managerScope = await accessScope.GetEffectiveScopeAsync(orgA.Id, manager.Id);
        Assert.False(managerScope.AllFarms);
        Assert.Contains(farmGo.Id, managerScope.FarmIds);
        Assert.DoesNotContain(farmPr.Id, managerScope.FarmIds);
        Assert.DoesNotContain(farmOtherOrg.Id, managerScope.FarmIds);

        Assert.NotNull(await farmService.GetAsync(orgA.Id, manager.Id, farmGo.Id));
        Assert.Null(await farmService.GetAsync(orgA.Id, manager.Id, farmPr.Id));
        Assert.Null(await farmService.GetAsync(orgA.Id, manager.Id, farmOtherOrg.Id));

        var managerList = await farmService.ListAsync(orgA.Id, manager.Id, 1, 20, null, false, null, null);
        Assert.Single(managerList.Items);
        Assert.Equal(farmGo.Id, managerList.Items[0].Id);

        var ownerList = await farmService.ListAsync(orgA.Id, owner.Id, 1, 20, null, false, null, null);
        Assert.Equal(2, ownerList.TotalCount);

        var stateFilter = await farmService.ListAsync(orgA.Id, owner.Id, 1, 20, null, false, null, "PR");
        Assert.Single(stateFilter.Items);
        Assert.Equal("PR", stateFilter.Items[0].StateCode);
    }
}
