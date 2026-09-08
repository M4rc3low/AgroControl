using AgroControl.Domain.Modules.Exporting;

namespace AgroControl.UnitTests;

public sealed class ExportDomainTests
{
    private static ExportOrder CreateOrder() => ExportOrder.Create(Guid.NewGuid(), "EXP-001", "Buyer", null, "US",
        null, null, null, null, "Soybeans", 100m, "t", "USD", 500m, 5.2m, IncotermCode.FOB,
        "Santos", "New Orleans", new DateOnly(2026, 9, 20), new DateOnly(2026, 10, 10), null, null, null, null, DateTime.UtcNow);

    [Fact]
    public void Order_calculates_commercial_value_and_brl_snapshot()
    {
        var order = CreateOrder();
        Assert.Equal(50_000m, order.CommercialValue);
        Assert.Equal(260_000m, order.EstimatedValueBrl);
    }

    [Fact]
    public void Order_rejects_invalid_currency_and_country_codes()
    {
        Assert.Throws<ArgumentException>(() => ExportOrder.Create(Guid.NewGuid(), "EXP-001", "Buyer", null, "USA",
            null, null, null, null, "Soybeans", 100m, "t", "US", 500m, 5m, IncotermCode.FOB,
            null, null, null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Status_transition_rejects_skipping_from_draft_to_in_transit()
    {
        var order = CreateOrder();
        Assert.Throws<InvalidOperationException>(() => order.TransitionTo(ExportOrderStatus.InTransit, new DateOnly(2026, 9, 10), DateTime.UtcNow));
    }

    [Fact]
    public void Cost_preserves_exchange_rate_snapshot()
    {
        var cost = ExportCost.Create(Guid.NewGuid(), Guid.NewGuid(), ExportCostType.Freight, "Ocean freight",
            10_000m, "USD", 5.1m, new DateOnly(2026, 9, 10), null, DateTime.UtcNow);
        Assert.Equal(51_000m, cost.AmountBrl);
        Assert.Equal(5.1m, cost.ExchangeRateToBrl);
    }
}
