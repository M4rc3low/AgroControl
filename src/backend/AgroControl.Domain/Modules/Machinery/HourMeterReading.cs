namespace AgroControl.Domain.Modules.Machinery;

public sealed class HourMeterReading
{
    private HourMeterReading() { }

    private HourMeterReading(Guid id, Guid organizationId, Guid machineId, decimal hours, DateTime occurredAtUtc, string? notes, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        MachineId = machineId;
        Hours = hours;
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid MachineId { get; private set; }
    public decimal Hours { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static HourMeterReading Create(Guid organizationId, Guid machineId, decimal hours, DateTime occurredAtUtc, string? notes, DateTime nowUtc)
    {
        if (machineId == Guid.Empty) throw new ArgumentException("Machine id is required.", nameof(machineId));
        if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours), "Hour meter cannot be negative.");
        return new HourMeterReading(Guid.NewGuid(), organizationId, machineId, hours, occurredAtUtc, Normalize(notes), nowUtc);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
