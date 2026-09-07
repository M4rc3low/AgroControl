using AgroControl.Application.Market;
using AgroControl.Domain.Modules.Market;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class MarketRepository(AgroControlDbContext dbContext) : IMarketRepository
{
    public async Task<(IReadOnlyList<Commodity> Items, int TotalCount)> ListCommoditiesAsync(
        Guid organizationId, int skip, int take, string? search, bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Commodities.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Symbol.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Commodity?> GetCommodityAsync(Guid organizationId, Guid commodityId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Commodity> query = dbContext.Commodities.Where(x => x.OrganizationId == organizationId && x.Id == commodityId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> SymbolExistsAsync(Guid organizationId, string symbol, Guid? excludingId, CancellationToken cancellationToken = default)
    {
        var normalized = symbol.ToUpper();
        return dbContext.Commodities.AsNoTracking().AnyAsync(
            x => x.OrganizationId == organizationId && x.Symbol.ToUpper() == normalized &&
                 (excludingId == null || x.Id != excludingId), cancellationToken);
    }

    public void AddCommodity(Commodity commodity) => dbContext.Commodities.Add(commodity);

    public void AddQuote(MarketQuote quote) => dbContext.MarketQuotes.Add(quote);

    public async Task<(IReadOnlyList<MarketQuote> Items, int TotalCount)> ListQuotesAsync(
        Guid organizationId, Guid commodityId, int skip, int take, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MarketQuotes.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CommodityId == commodityId);
        if (fromUtc is not null) query = query.Where(x => x.QuotedAtUtc >= fromUtc.Value);
        if (toUtc is not null) query = query.Where(x => x.QuotedAtUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.QuotedAtUtc).ThenByDescending(x => x.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<MarketQuote>> GetLatestQuotesAsync(
        Guid organizationId, Guid commodityId, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.MarketQuotes.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CommodityId == commodityId)
            .OrderByDescending(x => x.QuotedAtUtc).ThenByDescending(x => x.CreatedAtUtc)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);
    }

    public void AddAlert(PriceAlert alert) => dbContext.PriceAlerts.Add(alert);

    public Task<PriceAlert?> GetAlertAsync(Guid organizationId, Guid alertId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<PriceAlert> query = dbContext.PriceAlerts.Where(x => x.OrganizationId == organizationId && x.Id == alertId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<PriceAlert> Items, int TotalCount)> ListAlertsAsync(
        Guid organizationId, int skip, int take, Guid? commodityId, bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PriceAlerts.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (commodityId is not null) query = query.Where(x => x.CommodityId == commodityId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.CommodityId).ThenBy(x => x.TargetPrice)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}
