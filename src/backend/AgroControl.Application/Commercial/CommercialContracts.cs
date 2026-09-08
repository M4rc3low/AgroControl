using AgroControl.Domain.Modules.Commercial;

namespace AgroControl.Application.Commercial;

public sealed record CreateCommercialCustomerCommand(string Name, string? TradeName, string? TaxId, string? Email,
    string? Phone, string? CountryCode, string? City, string? State, CustomerStatus Status, string? Notes);
public sealed record UpdateCommercialCustomerCommand(string Name, string? TradeName, string? TaxId, string? Email,
    string? Phone, string? CountryCode, string? City, string? State, CustomerStatus Status, string? Notes);
public sealed record CommercialCustomerDto(Guid Id, string Name, string? TradeName, string? TaxId, string? Email,
    string? Phone, string? CountryCode, string? City, string? State, CustomerStatus Status, string? Notes,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateCommercialContactCommand(string Name, string? Role, string? Email, string? Phone, bool IsPrimary);
public sealed record UpdateCommercialContactCommand(string Name, string? Role, string? Email, string? Phone, bool IsPrimary);
public sealed record CommercialContactDto(Guid Id, Guid CustomerId, string Name, string? Role, string? Email,
    string? Phone, bool IsPrimary, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateCommercialOpportunityCommand(Guid CustomerId, string Title, Guid? FarmId, Guid? CropId,
    Guid? SeasonId, Guid? ExportOrderId, decimal ExpectedValue, string Currency, decimal ProbabilityPercent,
    DateOnly? ExpectedCloseDate, string? OwnerName, string? NextStep, string? Notes);
public sealed record UpdateCommercialOpportunityCommand(string Title, Guid? FarmId, Guid? CropId, Guid? SeasonId,
    Guid? ExportOrderId, decimal ExpectedValue, string Currency, decimal ProbabilityPercent,
    DateOnly? ExpectedCloseDate, string? OwnerName, string? NextStep, string? Notes);
public sealed record TransitionOpportunityStageCommand(OpportunityStage Stage, DateOnly OccurredOn, string? Notes);
public sealed record CommercialOpportunityDto(Guid Id, Guid CustomerId, string Title, Guid? FarmId, Guid? CropId,
    Guid? SeasonId, Guid? ExportOrderId, decimal ExpectedValue, string Currency, decimal ProbabilityPercent,
    decimal WeightedValue, OpportunityStage Stage, DateOnly? ExpectedCloseDate, DateOnly? ClosedOn,
    string? OwnerName, string? NextStep, string? Notes, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record OpportunityStageEventDto(Guid Id, Guid OpportunityId, OpportunityStage? FromStage,
    OpportunityStage ToStage, DateOnly OccurredOn, string? Notes, DateTime CreatedAtUtc);

public sealed record CommercialCurrencySummaryDto(string Currency, int OpenCount, decimal OpenPipelineValue,
    decimal WeightedPipelineValue, int WonCount, decimal WonValue);
public sealed record CommercialStageSummaryDto(OpportunityStage Stage, int Count);
public sealed record CommercialSummaryDto(DateOnly? From, DateOnly? To, int OpportunityCount, int OpenCount,
    int WonCount, int LostCount, decimal ConversionRatePercent,
    IReadOnlyList<CommercialCurrencySummaryDto> ByCurrency, IReadOnlyList<CommercialStageSummaryDto> ByStage);
