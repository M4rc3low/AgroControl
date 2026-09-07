using AgroControl.Domain.Modules.Machinery;

namespace AgroControl.Application.Machinery;

public sealed record CreateMachineCommand(
    string Name,
    string InternalCode,
    MachineKind Kind,
    Guid? FarmId,
    string? Manufacturer,
    string? Model,
    int? Year,
    decimal InitialHourMeter = 0m);

public sealed record UpdateMachineCommand(
    string Name,
    string InternalCode,
    MachineKind Kind,
    Guid? FarmId,
    string? Manufacturer,
    string? Model,
    int? Year,
    MachineStatus Status);

public sealed record MachineDto(
    Guid Id,
    Guid? FarmId,
    string Name,
    string InternalCode,
    MachineKind Kind,
    string? Manufacturer,
    string? Model,
    int? Year,
    MachineStatus Status,
    decimal CurrentHourMeter,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record RecordHourMeterCommand(decimal Hours, DateTime? OccurredAtUtc = null, string? Notes = null);

public sealed record HourMeterReadingDto(
    Guid Id,
    Guid MachineId,
    decimal Hours,
    DateTime OccurredAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateFuelingCommand(
    decimal Liters,
    decimal TotalCost,
    decimal HourMeter,
    DateTime? OccurredAtUtc = null,
    string? Notes = null);

public sealed record FuelingDto(
    Guid Id,
    Guid MachineId,
    decimal Liters,
    decimal TotalCost,
    decimal UnitCost,
    decimal HourMeter,
    DateTime OccurredAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateMaintenanceCommand(
    MaintenanceKind Kind,
    string Description,
    DateOnly PerformedOn,
    decimal? HourMeter,
    decimal PartsCost,
    decimal LaborCost,
    decimal OtherCost,
    DateOnly? NextMaintenanceDate,
    decimal? NextMaintenanceHourMeter,
    string? Notes = null);

public sealed record MaintenanceDto(
    Guid Id,
    Guid MachineId,
    MaintenanceKind Kind,
    string Description,
    DateOnly PerformedOn,
    decimal? HourMeter,
    decimal PartsCost,
    decimal LaborCost,
    decimal OtherCost,
    decimal TotalCost,
    DateOnly? NextMaintenanceDate,
    decimal? NextMaintenanceHourMeter,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record MachineryCostSummaryDto(
    Guid MachineId,
    decimal CurrentHourMeter,
    decimal FuelLiters,
    decimal FuelCost,
    decimal MaintenanceCost,
    decimal TotalCost,
    decimal TrackedHours,
    decimal? CostPerTrackedHour);
