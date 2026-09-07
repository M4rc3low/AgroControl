namespace AgroControl.Domain.Modules.Market;

public sealed class PriceAlert
{
    private PriceAlert() { }

    private PriceAlert(Guid id, Guid organizationId, Guid commodityId, decimal targetPrice, PriceAlertDirection direction, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CommodityId = commodityId;
        TargetPrice = targetPrice;
        Direction = direction;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CommodityId { get; private set; }
    public decimal TargetPrice { get; private set; }
    public PriceAlertDirection Direction { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static PriceAlert Create(Guid organizationId, Guid commodityId, decimal targetPrice, PriceAlertDirection direction, DateTime nowUtc)
    {
        if (commodityId == Guid.Empty) throw new ArgumentException("Commodity id is required.", nameof(commodityId));
        if (targetPrice <= 0) throw new ArgumentOutOfRangeException(nameof(targetPrice), "Target price must be greater than zero.");
        return new PriceAlert(Guid.NewGuid(), organizationId, commodityId, targetPrice, direction, nowUtc);
    }

    public bool IsTriggered(decimal? latestPrice) => latestPrice is not null && Direction switch
    {
        PriceAlertDirection.AboveOrEqual => latestPrice.Value >= TargetPrice,
        PriceAlertDirection.BelowOrEqual => latestPrice.Value <= TargetPrice,
        _ => false
    };

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }
}
