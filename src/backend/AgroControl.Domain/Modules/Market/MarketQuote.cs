namespace AgroControl.Domain.Modules.Market;

public sealed class MarketQuote
{
    private MarketQuote() { }

    private MarketQuote(Guid id, Guid organizationId, Guid commodityId, decimal price, string currency, string unit, string source, DateTime quotedAtUtc, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CommodityId = commodityId;
        Price = price;
        Currency = currency;
        Unit = unit;
        Source = source;
        QuotedAtUtc = quotedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CommodityId { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public DateTime QuotedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static MarketQuote Create(Guid organizationId, Guid commodityId, decimal price, string currency, string unit, string source, DateTime quotedAtUtc, DateTime nowUtc)
    {
        if (commodityId == Guid.Empty) throw new ArgumentException("Commodity id is required.", nameof(commodityId));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Quote price must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Quote currency is required.", nameof(currency));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Quote unit is required.", nameof(unit));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Quote source is required.", nameof(source));
        return new MarketQuote(Guid.NewGuid(), organizationId, commodityId, price, currency.Trim().ToUpperInvariant(), unit.Trim(), source.Trim(), quotedAtUtc, nowUtc);
    }
}
