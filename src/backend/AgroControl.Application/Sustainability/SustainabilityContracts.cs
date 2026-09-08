using AgroControl.Domain.Modules.Sustainability;

namespace AgroControl.Application.Sustainability;

public sealed record CreateEmissionFactorCommand(
    string Name,
    EmissionSourceCategory Category,
    string Unit,
    decimal KgCo2ePerUnit,
    string MethodologyReference,
    string? Notes,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

public sealed record UpdateEmissionFactorCommand(
    string Name,
    EmissionSourceCategory Category,
    string Unit,
    decimal KgCo2ePerUnit,
    string MethodologyReference,
    string? Notes,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

public sealed record EmissionFactorDto(
    Guid Id,
    string Name,
    EmissionSourceCategory Category,
    string Unit,
    decimal KgCo2ePerUnit,
    string MethodologyReference,
    string? Notes,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateEmissionActivityCommand(
    Guid EmissionFactorId,
    decimal Quantity,
    DateOnly ActivityDate,
    EmissionActivityOrigin Origin,
    SustainabilityDataQuality DataQuality,
    string Description,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    string? SourceModule,
    string? SourceReferenceId,
    string? Notes);

public sealed record EmissionActivityDto(
    Guid Id,
    Guid EmissionFactorId,
    string FactorName,
    EmissionSourceCategory Category,
    string Unit,
    decimal FactorKgCo2ePerUnit,
    decimal Quantity,
    decimal EmissionsKgCo2e,
    decimal EmissionsTCo2e,
    DateOnly ActivityDate,
    EmissionActivityOrigin Origin,
    SustainabilityDataQuality DataQuality,
    string Description,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    string? SourceModule,
    string? SourceReferenceId,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record SustainabilityBreakdownProjection(
    EmissionSourceCategory Category,
    int ActivityCount,
    decimal TotalKgCo2e);

public sealed record SustainabilityAggregateProjection(
    int ActivityCount,
    int EstimatedActivityCount,
    decimal TotalKgCo2e,
    IReadOnlyList<SustainabilityBreakdownProjection> Breakdown);

public sealed record SustainabilityBreakdownDto(
    EmissionSourceCategory Category,
    int ActivityCount,
    decimal TotalKgCo2e,
    decimal TotalTCo2e,
    decimal SharePercent);

public sealed record SustainabilitySummaryDto(
    DateOnly? From,
    DateOnly? To,
    Guid? FarmId,
    Guid? FieldId,
    Guid? SeasonId,
    int ActivityCount,
    int EstimatedActivityCount,
    decimal TotalKgCo2e,
    decimal TotalTCo2e,
    IReadOnlyList<SustainabilityBreakdownDto> Breakdown,
    decimal? PreviousPeriodKgCo2e,
    decimal? ChangePercent);

public sealed record SeasonSustainabilitySummaryDto(
    Guid SeasonId,
    Guid FieldId,
    Guid FarmId,
    decimal AreaHectares,
    decimal? ActualYieldPerHectare,
    decimal? ProductionUnits,
    int ActivityCount,
    decimal TotalKgCo2e,
    decimal TotalTCo2e,
    decimal TCo2ePerHectare,
    decimal? KgCo2ePerUnitProduced,
    IReadOnlyList<SustainabilityBreakdownDto> Breakdown);
