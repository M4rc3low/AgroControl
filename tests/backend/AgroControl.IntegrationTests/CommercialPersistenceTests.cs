using AgroControl.Application.Commercial;
using AgroControl.Application.Exporting;
using AgroControl.Domain.Modules.Commercial;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.IntegrationTests;

public sealed class CommercialPersistenceTests
{
    [Fact]
    public async Task Commercial_pipeline_is_tenant_isolated_and_keeps_stage_history()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROCONTROL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var options = new DbContextOptionsBuilder<AgroControlDbContext>().UseNpgsql(connectionString).Options;
        await using var dbContext = new AgroControlDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var orgA = Organization.Create("Commercial A", $"commercial-a-{Guid.NewGuid():N}", now);
        var orgB = Organization.Create("Commercial B", $"commercial-b-{Guid.NewGuid():N}", now);
        dbContext.Organizations.AddRange(orgA, orgB);
        await dbContext.SaveChangesAsync();

        var repository = new CommercialRepository(dbContext);
        var service = new CommercialService(repository, new ProductionRepository(dbContext),
            new ExportRepository(dbContext), new UnitOfWork(dbContext));
        var customer = await service.CreateCustomerAsync(orgA.Id, new CreateCommercialCustomerCommand(
            "Cooperativa Verde", "Coop Verde", null, "compras@verde.test", null, "BR", "Rio Verde", "GO",
            CustomerStatus.Prospect, null));
        Assert.True(customer.Succeeded);

        var opportunity = await service.CreateOpportunityAsync(orgA.Id, new CreateCommercialOpportunityCommand(
            customer.Value!.Id, "Contrato soja 2026", null, null, null, null, 200_000m, "BRL", 50m,
            new DateOnly(2026, 10, 31), "Equipe comercial", "Enviar amostra", null));
        Assert.True(opportunity.Succeeded);
        Assert.Equal(100_000m, opportunity.Value!.WeightedValue);

        var listA = await service.ListOpportunitiesAsync(orgA.Id, 1, 20, null, null, null, null, null, null, null);
        var listB = await service.ListOpportunitiesAsync(orgB.Id, 1, 20, null, null, null, null, null, null, null);
        Assert.Single(listA.Items);
        Assert.Empty(listB.Items);

        Assert.True((await service.TransitionStageAsync(orgA.Id, opportunity.Value.Id,
            new TransitionOpportunityStageCommand(OpportunityStage.Qualification, new DateOnly(2026, 9, 9), "Qualificado"))).Succeeded);
        Assert.True((await service.TransitionStageAsync(orgA.Id, opportunity.Value.Id,
            new TransitionOpportunityStageCommand(OpportunityStage.Proposal, new DateOnly(2026, 9, 10), "Proposta enviada"))).Succeeded);
        Assert.True((await service.TransitionStageAsync(orgA.Id, opportunity.Value.Id,
            new TransitionOpportunityStageCommand(OpportunityStage.Won, new DateOnly(2026, 9, 11), "Fechado"))).Succeeded);

        var timeline = await service.GetTimelineAsync(orgA.Id, opportunity.Value.Id);
        Assert.True(timeline.Succeeded);
        Assert.Equal(4, timeline.Value!.Count);
        var summary = await service.GetSummaryAsync(orgA.Id, null, null, null, null);
        Assert.Equal(1, summary.WonCount);
        Assert.Equal(100m, summary.ConversionRatePercent);
        Assert.Equal(200_000m, summary.ByCurrency.Single().WonValue);
    }
}
