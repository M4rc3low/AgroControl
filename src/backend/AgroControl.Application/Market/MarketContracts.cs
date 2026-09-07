using AgroControl.Domain.Modules.Market;

namespace AgroControl.Application.Market;

public sealed record CreateCommodityCommand(string Name, string Symbol, string DefaultCurrency, string DefaultUnit);
public sealed record UpdateCommodityCommand(string Name, string Symbol, string DefaultCurrency, string DefaultUnit);

public sealed record CommodityDto(
    Guid Id,
    string Name,
    string Symbol,
    string DefaultCurrency,
    string DefaultUnit,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record AddMarketQuoteCommand(
    decimal Price,
    string? Currency,
    string? Unit,
    string Source,
    DateTime? QuotedAtUtc = null);

public sealed record MarketQuoteDto(
    Guid Id,
    Guid CommodityId,
    decimal Price,
    string Currency,
    string Unit,
    string Source,
    DateTime QuotedAtUtc,
    DateTime CreatedAtUtc);

public sealed record CommodityMarketSummaryDto(
    Guid CommodityId,
    MarketQuoteDto? Latest,
    MarketQuoteDto? Previous,
    decimal? AbsoluteVariation,
    decimal? PercentageVariation);

public sealed record CreatePriceAlertCommand(decimal TargetPrice, PriceAlertDirection Direction);

public sealed record PriceAlertDto(
    Guid Id,
    Guid CommodityId,
    decimal TargetPrice,
    PriceAlertDirection Direction,
    bool IsActive,
    decimal? LatestPrice,
    bool Triggered,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ExternalMarketQuote(
    string Symbol,
    decimal Price,
    string Currency,
    string Unit,
    string Source,
    DateTime QuotedAtUtc);

public interface IMarketQuoteProvider
{
    string ProviderKey { get; }
    Task<ExternalMarketQuote?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default);
}
