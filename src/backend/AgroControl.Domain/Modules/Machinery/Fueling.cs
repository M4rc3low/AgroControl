namespace AgroControl.Domain.Modules.Machinery;

public sealed class Fueling
{
    private Fueling() { }

    private Fueling(Guid id, Guid organizationId, Guid machineId, decimal liters, decimal totalCost, decimal hourMeter, DateTime occurredAtUtc, string? notes, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        MachineId = machineId;
        Liters = liters;
        TotalCost = totalCost;
        HourMeter = hourMeter;
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid MachineId { get; private set; }
    public decimal Liters { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal HourMeter { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal UnitCost => Liters <= 0 ? 0m : TotalCost / Liters;

    public static Fueling Create(Guid organizationId, Guid machineId, decimal liters, decimal totalCost, decimal hourMeter, DateTime occurredAtUtc, string? notes, DateTime nowUtc)
    {
        if (machineId == Guid.Empty) throw new ArgumentException("Machine id is required.", nameof(machineId));
        if (liters <= 0) throw new ArgumentOutOfRangeException(nameof(liters), "Fuel quantity must be greater than zero.");
        if (totalCost < 0) throw new ArgumentOutOfRangeException(nameof(totalCost), "Fuel cost cannot be negative.");
        if (hourMeter < 0) throw new ArgumentOutOfRangeException(nameof(hourMeter), "Hour meter cannot be negative.");
        return new Fueling(Guid.NewGuid(), organizationId, machineId, liters, totalCost, hourMeter, occurredAtUtc, Normalize(notes), nowUtc);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
