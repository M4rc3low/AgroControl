using AgroControl.Domain.Modules.Exporting;

namespace AgroControl.Application.Exporting;

public sealed record CreateExportOrderCommand(
    string OrderNumber,
    string BuyerName,
    string? BuyerReference,
    string DestinationCountryCode,
    Guid? FarmId,
    Guid? FieldId,
    Guid? CropId,
    Guid? SeasonId,
    string ProductDescription,
    decimal Quantity,
    string Unit,
    string Currency,
    decimal UnitPrice,
    decimal ExchangeRateToBrl,
    IncotermCode Incoterm,
    string? OriginLocation,
    string? DestinationLocation,
    DateOnly? EstimatedShipmentDate,
    DateOnly? EstimatedDeliveryDate,
    string? ShipmentReference,
    string? BookingReference,
    string? ContainerReference,
    string? Notes);

public sealed record UpdateExportOrderCommand(
    string OrderNumber,
    string BuyerName,
    string? BuyerReference,
    string DestinationCountryCode,
    Guid? FarmId,
    Guid? FieldId,
    Guid? CropId,
    Guid? SeasonId,
    string ProductDescription,
    decimal Quantity,
    string Unit,
    string Currency,
    decimal UnitPrice,
    decimal ExchangeRateToBrl,
    IncotermCode Incoterm,
    string? OriginLocation,
    string? DestinationLocation,
    DateOnly? EstimatedShipmentDate,
    DateOnly? EstimatedDeliveryDate,
    string? ShipmentReference,
    string? BookingReference,
    string? ContainerReference,
    string? Notes);

public sealed record TransitionExportOrderStatusCommand(ExportOrderStatus Status, DateOnly OccurredOn, string? Notes);

public sealed record ExportOrderDto(
    Guid Id,
    string OrderNumber,
    string BuyerName,
    string? BuyerReference,
    string DestinationCountryCode,
    Guid? FarmId,
    Guid? FieldId,
    Guid? CropId,
    Guid? SeasonId,
    string ProductDescription,
    decimal Quantity,
    string Unit,
    string Currency,
    decimal UnitPrice,
    decimal CommercialValue,
    decimal ExchangeRateToBrl,
    decimal EstimatedValueBrl,
    IncotermCode Incoterm,
    ExportOrderStatus Status,
    string? OriginLocation,
    string? DestinationLocation,
    DateOnly? ContractedOn,
    DateOnly? EstimatedShipmentDate,
    DateOnly? ActualShipmentDate,
    DateOnly? EstimatedDeliveryDate,
    DateOnly? ActualDeliveryDate,
    string? ShipmentReference,
    string? BookingReference,
    string? ContainerReference,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateExportDocumentCommand(ExportDocumentType Type, string? CustomLabel);
public sealed record UpdateExportDocumentCommand(ExportDocumentStatus Status, string? ReferenceNumber, DateOnly? IssuedOn, string? Notes);
public sealed record ExportDocumentDto(Guid Id, Guid OrderId, ExportDocumentType Type, string? CustomLabel,
    ExportDocumentStatus Status, string? ReferenceNumber, DateOnly? IssuedOn, string? Notes,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CreateExportCostCommand(ExportCostType Type, string Description, decimal Amount, string Currency,
    decimal ExchangeRateToBrl, DateOnly IncurredOn, string? Notes);
public sealed record ExportCostDto(Guid Id, Guid OrderId, ExportCostType Type, string Description, decimal Amount,
    string Currency, decimal ExchangeRateToBrl, decimal AmountBrl, DateOnly IncurredOn, string? Notes, DateTime CreatedAtUtc);

public sealed record ExportStatusEventDto(Guid Id, Guid OrderId, ExportOrderStatus? FromStatus, ExportOrderStatus ToStatus,
    DateOnly OccurredOn, string? Notes, DateTime CreatedAtUtc);

public sealed record ExportSummaryBreakdownDto(string Key, int OrderCount, decimal EstimatedValueBrl);
public sealed record ExportSummaryDto(
    DateOnly? From,
    DateOnly? To,
    int OrderCount,
    int ContractedCount,
    int InTransitCount,
    int DeliveredCount,
    decimal EstimatedCommercialValueBrl,
    decimal TotalLogisticsCostBrl,
    decimal EstimatedOperationalMarginBrl,
    IReadOnlyList<ExportSummaryBreakdownDto> ByStatus,
    IReadOnlyList<ExportSummaryBreakdownDto> ByCountry,
    IReadOnlyList<ExportSummaryBreakdownDto> ByCurrency);
