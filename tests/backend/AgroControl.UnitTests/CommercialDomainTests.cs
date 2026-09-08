using AgroControl.Domain.Modules.Commercial;

namespace AgroControl.UnitTests;

public sealed class CommercialDomainTests
{
    private static CommercialOpportunity CreateOpportunity(decimal probability = 40m) =>
        CommercialOpportunity.Create(Guid.NewGuid(), Guid.NewGuid(), "Venda de soja", null, null, null, null,
            100_000m, "BRL", probability, new DateOnly(2026, 10, 30), "Marcelo", "Enviar proposta", null, DateTime.UtcNow);

    [Fact]
    public void Customer_requires_name()
    {
        Assert.Throws<ArgumentException>(() => CommercialCustomer.Create(Guid.NewGuid(), " ", null, null, null,
            null, null, null, null, CustomerStatus.Lead, null, DateTime.UtcNow));
    }

    [Fact]
    public void Opportunity_calculates_weighted_value()
    {
        var opportunity = CreateOpportunity(35m);
        Assert.Equal(35_000m, opportunity.WeightedValue);
    }

    [Fact]
    public void Opportunity_rejects_probability_outside_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateOpportunity(101m));
    }

    [Fact]
    public void Won_opportunity_becomes_terminal_and_probability_is_one_hundred()
    {
        var opportunity = CreateOpportunity();
        opportunity.TransitionTo(OpportunityStage.Qualification, new DateOnly(2026, 9, 8), DateTime.UtcNow);
        opportunity.TransitionTo(OpportunityStage.Proposal, new DateOnly(2026, 9, 9), DateTime.UtcNow);
        opportunity.TransitionTo(OpportunityStage.Won, new DateOnly(2026, 9, 10), DateTime.UtcNow);
        Assert.True(opportunity.IsTerminal);
        Assert.Equal(100m, opportunity.ProbabilityPercent);
        Assert.Throws<InvalidOperationException>(() => opportunity.Update("Outra", null, null, null, null, 1m,
            "BRL", 100m, null, null, null, null, DateTime.UtcNow));
    }
}
