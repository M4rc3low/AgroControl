using AgroControl.Domain.Modules.Market;

namespace AgroControl.Application.Market;

public interface IMarketRepository
{
    Task<(IReadOnlyList<Commodity> Items, int TotalCount)> ListCommoditiesAsync(
        Guid organizationId, int skip, int take, string? search, bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<Commodity?> GetCommodityAsync(Guid organizationId, Guid commodityId, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> SymbolExistsAsync(Guid organizationId, string symbol, Guid? excludingId, CancellationToken cancellationToken = default);
    void AddCommodity(Commodity commodity);

    void AddQuote(MarketQuote quote);
    Task<(IReadOnlyList<MarketQuote> Items, int TotalCount)> ListQuotesAsync(
        Guid organizationId, Guid commodityId, int skip, int take, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarketQuote>> GetLatestQuotesAsync(
        Guid organizationId, Guid commodityId, int take, CancellationToken cancellationToken = default);

    void AddAlert(PriceAlert alert);
    Task<PriceAlert?> GetAlertAsync(Guid organizationId, Guid alertId, bool tracking, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PriceAlert> Items, int TotalCount)> ListAlertsAsync(
        Guid organizationId, int skip, int take, Guid? commodityId, bool includeInactive,
        CancellationToken cancellationToken = default);
}
