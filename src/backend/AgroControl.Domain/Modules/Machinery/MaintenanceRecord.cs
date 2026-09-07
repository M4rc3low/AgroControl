namespace AgroControl.Domain.Modules.Machinery;

public sealed class MaintenanceRecord
{
    private MaintenanceRecord() { }

    private MaintenanceRecord(
        Guid id,
        Guid organizationId,
        Guid machineId,
        MaintenanceKind kind,
        string description,
        DateOnly performedOn,
        decimal? hourMeter,
        decimal partsCost,
        decimal laborCost,
        decimal otherCost,
        DateOnly? nextMaintenanceDate,
        decimal? nextMaintenanceHourMeter,
        string? notes,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        MachineId = machineId;
        Kind = kind;
        Description = description;
        PerformedOn = performedOn;
        HourMeter = hourMeter;
        PartsCost = partsCost;
        LaborCost = laborCost;
        OtherCost = otherCost;
        NextMaintenanceDate = nextMaintenanceDate;
        NextMaintenanceHourMeter = nextMaintenanceHourMeter;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid MachineId { get; private set; }
    public MaintenanceKind Kind { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateOnly PerformedOn { get; private set; }
    public decimal? HourMeter { get; private set; }
    public decimal PartsCost { get; private set; }
    public decimal LaborCost { get; private set; }
    public decimal OtherCost { get; private set; }
    public DateOnly? NextMaintenanceDate { get; private set; }
    public decimal? NextMaintenanceHourMeter { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal TotalCost => PartsCost + LaborCost + OtherCost;

    public static MaintenanceRecord Create(
        Guid organizationId,
        Guid machineId,
        MaintenanceKind kind,
        string description,
        DateOnly performedOn,
        decimal? hourMeter,
        decimal partsCost,
        decimal laborCost,
        decimal otherCost,
        DateOnly? nextMaintenanceDate,
        decimal? nextMaintenanceHourMeter,
        string? notes,
        DateTime nowUtc)
    {
        if (machineId == Guid.Empty) throw new ArgumentException("Machine id is required.", nameof(machineId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Maintenance description is required.", nameof(description));
        if (hourMeter is < 0) throw new ArgumentOutOfRangeException(nameof(hourMeter), "Hour meter cannot be negative.");
        if (partsCost < 0 || laborCost < 0 || otherCost < 0) throw new ArgumentOutOfRangeException(nameof(partsCost), "Maintenance costs cannot be negative.");
        if (nextMaintenanceHourMeter is <= 0) throw new ArgumentOutOfRangeException(nameof(nextMaintenanceHourMeter), "Next maintenance hour meter must be greater than zero.");
        if (hourMeter is not null && nextMaintenanceHourMeter is not null && nextMaintenanceHourMeter <= hourMeter)
            throw new ArgumentException("Next maintenance hour meter must be greater than the performed maintenance hour meter.", nameof(nextMaintenanceHourMeter));
        if (nextMaintenanceDate is not null && nextMaintenanceDate < performedOn)
            throw new ArgumentException("Next maintenance date cannot be before the performed date.", nameof(nextMaintenanceDate));

        return new MaintenanceRecord(
            Guid.NewGuid(), organizationId, machineId, kind, description.Trim(), performedOn, hourMeter,
            partsCost, laborCost, otherCost, nextMaintenanceDate, nextMaintenanceHourMeter, Normalize(notes), nowUtc);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
