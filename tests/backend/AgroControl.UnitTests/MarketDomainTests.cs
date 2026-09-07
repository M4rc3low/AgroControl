using AgroControl.Domain.Modules.Market;

namespace AgroControl.UnitTests;

public sealed class MarketDomainTests
{
    [Fact]
    public void Price_alert_detects_target_condition()
    {
        var alert = PriceAlert.Create(Guid.NewGuid(), Guid.NewGuid(), 130m, PriceAlertDirection.AboveOrEqual, DateTime.UtcNow);

        Assert.False(alert.IsTriggered(129.99m));
        Assert.True(alert.IsTriggered(130m));
        Assert.True(alert.IsTriggered(135m));
    }

    [Fact]
    public void Quote_requires_positive_price()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarketQuote.Create(
            Guid.NewGuid(), Guid.NewGuid(), 0m, "BRL", "sc", "Manual", DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Commodity_normalizes_symbol_and_currency()
    {
        var commodity = Commodity.Create(Guid.NewGuid(), "Soja", " soja ", " brl ", "saca", DateTime.UtcNow);

        Assert.Equal("SOJA", commodity.Symbol);
        Assert.Equal("BRL", commodity.DefaultCurrency);
    }
}
