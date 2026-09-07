using AgroControl.Application.Common;
using AgroControl.Domain.Modules.Market;

namespace AgroControl.Application.Market;

public sealed class MarketService(IMarketRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<CommodityDto>> ListCommoditiesAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListCommoditiesAsync(
            organizationId, (page - 1) * pageSize, pageSize, search, includeInactive, cancellationToken);
        return new PagedResult<CommodityDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<CommodityDto?> GetCommodityAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var commodity = await repository.GetCommodityAsync(organizationId, id, false, cancellationToken);
        return commodity is null ? null : ToDto(commodity);
    }

    public async Task<OperationResult<CommodityDto>> CreateCommodityAsync(
        Guid organizationId,
        CreateCommodityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Symbol)) return OperationResult<CommodityDto>.Validation("Commodity symbol is required.");
        if (await repository.SymbolExistsAsync(organizationId, command.Symbol.Trim(), null, cancellationToken))
            return OperationResult<CommodityDto>.Conflict("A commodity with this symbol already exists.");

        try
        {
            var commodity = Commodity.Create(
                organizationId, command.Name, command.Symbol, command.DefaultCurrency, command.DefaultUnit, DateTime.UtcNow);
            repository.AddCommodity(commodity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommodityDto>.Success(ToDto(commodity));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<CommodityDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<CommodityDto>> UpdateCommodityAsync(
        Guid organizationId,
        Guid id,
        UpdateCommodityCommand command,
        CancellationToken cancellationToken = default)
    {
        var commodity = await repository.GetCommodityAsync(organizationId, id, true, cancellationToken);
        if (commodity is null) return OperationResult<CommodityDto>.NotFound("Commodity not found.");
        if (string.IsNullOrWhiteSpace(command.Symbol)) return OperationResult<CommodityDto>.Validation("Commodity symbol is required.");
        if (await repository.SymbolExistsAsync(organizationId, command.Symbol.Trim(), id, cancellationToken))
            return OperationResult<CommodityDto>.Conflict("A commodity with this symbol already exists.");

        try
        {
            commodity.Update(command.Name, command.Symbol, command.DefaultCurrency, command.DefaultUnit, DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<CommodityDto>.Success(ToDto(commodity));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<CommodityDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> DeactivateCommodityAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var commodity = await repository.GetCommodityAsync(organizationId, id, true, cancellationToken);
        if (commodity is null) return OperationResult<bool>.NotFound("Commodity not found.");
        commodity.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<MarketQuoteDto>> AddQuoteAsync(
        Guid organizationId,
        Guid commodityId,
        AddMarketQuoteCommand command,
        CancellationToken cancellationToken = default)
    {
        var commodity = await repository.GetCommodityAsync(organizationId, commodityId, false, cancellationToken);
        if (commodity is null) return OperationResult<MarketQuoteDto>.NotFound("Commodity not found.");
        if (!commodity.IsActive) return OperationResult<MarketQuoteDto>.Conflict("Inactive commodities cannot receive new quotes.");

        try
        {
            var now = DateTime.UtcNow;
            var quote = MarketQuote.Create(
                organizationId,
                commodityId,
                command.Price,
                string.IsNullOrWhiteSpace(command.Currency) ? commodity.DefaultCurrency : command.Currency,
                string.IsNullOrWhiteSpace(command.Unit) ? commodity.DefaultUnit : command.Unit,
                command.Source,
                command.QuotedAtUtc ?? now,
                now);
            repository.AddQuote(quote);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return OperationResult<MarketQuoteDto>.Success(ToDto(quote));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<MarketQuoteDto>.Validation(ex.Message);
        }
    }

    public async Task<OperationResult<PagedResult<MarketQuoteDto>>> ListQuotesAsync(
        Guid organizationId,
        Guid commodityId,
        int page,
        int pageSize,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetCommodityAsync(organizationId, commodityId, false, cancellationToken) is null)
            return OperationResult<PagedResult<MarketQuoteDto>>.NotFound("Commodity not found.");
        if (fromUtc is not null && toUtc is not null && fromUtc > toUtc)
            return OperationResult<PagedResult<MarketQuoteDto>>.Validation("The quote start date cannot be after the end date.");

        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListQuotesAsync(
            organizationId, commodityId, (page - 1) * pageSize, pageSize, fromUtc, toUtc, cancellationToken);
        return OperationResult<PagedResult<MarketQuoteDto>>.Success(
            new PagedResult<MarketQuoteDto>(items.Select(ToDto).ToList(), page, pageSize, total));
    }

    public async Task<OperationResult<CommodityMarketSummaryDto>> GetSummaryAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetCommodityAsync(organizationId, commodityId, false, cancellationToken) is null)
            return OperationResult<CommodityMarketSummaryDto>.NotFound("Commodity not found.");

        var quotes = await repository.GetLatestQuotesAsync(organizationId, commodityId, 2, cancellationToken);
        var latest = quotes.Count > 0 ? quotes[0] : null;
        var previous = quotes.Count > 1 ? quotes[1] : null;
        decimal? absoluteVariation = null;
        decimal? percentageVariation = null;
        if (latest is not null && previous is not null)
        {
            absoluteVariation = latest.Price - previous.Price;
            if (previous.Price != 0m)
                percentageVariation = absoluteVariation.Value / previous.Price * 100m;
        }

        return OperationResult<CommodityMarketSummaryDto>.Success(new CommodityMarketSummaryDto(
            commodityId,
            latest is null ? null : ToDto(latest),
            previous is null ? null : ToDto(previous),
            absoluteVariation,
            percentageVariation));
    }

    public async Task<OperationResult<PriceAlertDto>> CreateAlertAsync(
        Guid organizationId,
        Guid commodityId,
        CreatePriceAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var commodity = await repository.GetCommodityAsync(organizationId, commodityId, false, cancellationToken);
        if (commodity is null) return OperationResult<PriceAlertDto>.NotFound("Commodity not found.");
        if (!commodity.IsActive) return OperationResult<PriceAlertDto>.Conflict("Inactive commodities cannot receive new price alerts.");

        try
        {
            var alert = PriceAlert.Create(organizationId, commodityId, command.TargetPrice, command.Direction, DateTime.UtcNow);
            repository.AddAlert(alert);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var latest = await repository.GetLatestQuotesAsync(organizationId, commodityId, 1, cancellationToken);
            var latestPrice = latest.Count == 0 ? (decimal?)null : latest[0].Price;
            return OperationResult<PriceAlertDto>.Success(ToDto(alert, latestPrice));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<PriceAlertDto>.Validation(ex.Message);
        }
    }

    public async Task<PagedResult<PriceAlertDto>> ListAlertsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        Guid? commodityId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await repository.ListAlertsAsync(
            organizationId, (page - 1) * pageSize, pageSize, commodityId, includeInactive, cancellationToken);

        var latestPrices = new Dictionary<Guid, decimal?>();
        foreach (var id in items.Select(x => x.CommodityId).Distinct())
        {
            var quotes = await repository.GetLatestQuotesAsync(organizationId, id, 1, cancellationToken);
            latestPrices[id] = quotes.Count == 0 ? null : quotes[0].Price;
        }

        var dtos = items.Select(item => ToDto(item, latestPrices.GetValueOrDefault(item.CommodityId))).ToList();
        return new PagedResult<PriceAlertDto>(dtos, page, pageSize, total);
    }

    public async Task<OperationResult<bool>> DeactivateAlertAsync(
        Guid organizationId,
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        var alert = await repository.GetAlertAsync(organizationId, alertId, true, cancellationToken);
        if (alert is null) return OperationResult<bool>.NotFound("Price alert not found.");
        alert.Deactivate(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static CommodityDto ToDto(Commodity item) => new(
        item.Id, item.Name, item.Symbol, item.DefaultCurrency, item.DefaultUnit,
        item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);

    private static MarketQuoteDto ToDto(MarketQuote item) => new(
        item.Id, item.CommodityId, item.Price, item.Currency, item.Unit,
        item.Source, item.QuotedAtUtc, item.CreatedAtUtc);

    private static PriceAlertDto ToDto(PriceAlert item, decimal? latestPrice) => new(
        item.Id, item.CommodityId, item.TargetPrice, item.Direction, item.IsActive,
        latestPrice, item.IsTriggered(latestPrice), item.CreatedAtUtc, item.UpdatedAtUtc);

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}
