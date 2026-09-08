using AgroControl.Application.Exporting;
using AgroControl.Domain.Modules.Exporting;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class ExportRepository(AgroControlDbContext dbContext) : IExportRepository
{
    public async Task<(IReadOnlyList<ExportOrder> Items, int TotalCount)> ListOrdersAsync(Guid organizationId, int skip, int take,
        string? search, ExportOrderStatus? status, string? countryCode, Guid? seasonId, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = BuildOrderQuery(organizationId, from, to, status, countryCode, null);
        if (seasonId is not null) query = query.Where(x => x.SeasonId == seasonId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.OrderNumber, pattern)
                || EF.Functions.ILike(x.BuyerName, pattern)
                || EF.Functions.ILike(x.ProductDescription, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<ExportOrder?> GetOrderAsync(Guid organizationId, Guid orderId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<ExportOrder> query = tracking ? dbContext.ExportOrders : dbContext.ExportOrders.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == orderId, cancellationToken);
    }

    public Task<bool> OrderNumberExistsAsync(Guid organizationId, string orderNumber, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.ExportOrders.AnyAsync(x => x.OrganizationId == organizationId && x.OrderNumber == orderNumber
            && (excludingId == null || x.Id != excludingId.Value), cancellationToken);

    public void AddOrder(ExportOrder order) => dbContext.ExportOrders.Add(order);

    public async Task<IReadOnlyList<ExportDocument>> ListDocumentsAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default) =>
        await dbContext.ExportDocuments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.OrderId == orderId)
            .OrderBy(x => x.Type).ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ExportDocument?> GetDocumentAsync(Guid organizationId, Guid orderId, Guid documentId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<ExportDocument> query = tracking ? dbContext.ExportDocuments : dbContext.ExportDocuments.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.OrderId == orderId && x.Id == documentId, cancellationToken);
    }

    public void AddDocument(ExportDocument document) => dbContext.ExportDocuments.Add(document);

    public async Task<IReadOnlyList<ExportCost>> ListCostsAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default) =>
        await dbContext.ExportCosts.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.OrderId == orderId)
            .OrderByDescending(x => x.IncurredOn).ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void AddCost(ExportCost cost) => dbContext.ExportCosts.Add(cost);

    public async Task<IReadOnlyList<ExportOrderStatusEvent>> ListTimelineAsync(Guid organizationId, Guid orderId, CancellationToken cancellationToken = default) =>
        await dbContext.ExportOrderStatusEvents.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.OrderId == orderId)
            .OrderBy(x => x.OccurredOn).ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void AddStatusEvent(ExportOrderStatusEvent statusEvent) => dbContext.ExportOrderStatusEvents.Add(statusEvent);

    public async Task<IReadOnlyList<ExportOrder>> ListSummaryOrdersAsync(Guid organizationId, DateOnly? from, DateOnly? to,
        ExportOrderStatus? status, string? countryCode, string? currency, CancellationToken cancellationToken = default) =>
        await BuildOrderQuery(organizationId, from, to, status, countryCode, currency)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<decimal> SumCostsBrlAsync(Guid organizationId, IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default) =>
        await dbContext.ExportCosts.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && orderIds.Contains(x.OrderId))
            .SumAsync(x => (decimal?)x.AmountBrl, cancellationToken) ?? 0m;

    private IQueryable<ExportOrder> BuildOrderQuery(Guid organizationId, DateOnly? from, DateOnly? to,
        ExportOrderStatus? status, string? countryCode, string? currency)
    {
        var query = dbContext.ExportOrders.AsNoTracking().Where(x => x.OrganizationId == organizationId);

        if (from is not null)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAtUtc >= fromUtc);
        }

        if (to is not null)
        {
            var nextDayUtc = DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAtUtc < nextDayUtc);
        }

        if (status is not null) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(countryCode)) query = query.Where(x => x.DestinationCountryCode == countryCode);
        if (!string.IsNullOrWhiteSpace(currency)) query = query.Where(x => x.Currency == currency);
        return query;
    }
}
