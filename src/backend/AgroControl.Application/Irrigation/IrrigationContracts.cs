using AgroControl.Domain.Modules.Irrigation;

namespace AgroControl.Application.Irrigation;

public sealed record CreateIrrigationZoneCommand(
    Guid FieldId,
    string Name,
    decimal AreaHectares,
    IrrigationMethod Method,
    decimal MinimumMoisturePercent,
    decimal TargetMoisturePercent,
    decimal MaximumMoisturePercent,
    Guid? TelemetryDeviceId);

public sealed record UpdateIrrigationZoneCommand(
    Guid FieldId,
    string Name,
    decimal AreaHectares,
    IrrigationMethod Method,
    decimal MinimumMoisturePercent,
    decimal TargetMoisturePercent,
    decimal MaximumMoisturePercent,
    Guid? TelemetryDeviceId);

public sealed record IrrigationZoneDto(
    Guid Id,
    Guid FieldId,
    string Name,
    decimal AreaHectares,
    IrrigationMethod Method,
    decimal MinimumMoisturePercent,
    decimal TargetMoisturePercent,
    decimal MaximumMoisturePercent,
    Guid? TelemetryDeviceId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record IrrigationZoneStatusDto(
    Guid ZoneId,
    Guid FieldId,
    Guid? TelemetryDeviceId,
    bool TelemetryAvailable,
    bool HasReading,
    double? SoilMoisturePercent,
    DateTimeOffset? CapturedAtUtc,
    WaterCondition? Condition,
    IrrigationRecommendation Recommendation,
    string Message);

public sealed record CreateIrrigationApplicationCommand(
    Guid ZoneId,
    decimal DepthMillimeters,
    IrrigationApplicationSource Source,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? Notes);

public sealed record IrrigationApplicationDto(
    Guid Id,
    Guid ZoneId,
    Guid FieldId,
    decimal AreaHectaresSnapshot,
    decimal DepthMillimeters,
    decimal EstimatedVolumeCubicMeters,
    IrrigationApplicationSource Source,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record IrrigationSummaryProjection(
    int ApplicationCount,
    decimal TotalDepthMillimeters,
    decimal EstimatedVolumeCubicMeters);

public sealed record IrrigationSummaryDto(
    DateTime? FromUtc,
    DateTime? ToUtc,
    Guid? FieldId,
    Guid? ZoneId,
    int ApplicationCount,
    decimal TotalDepthMillimeters,
    decimal EstimatedVolumeCubicMeters);
