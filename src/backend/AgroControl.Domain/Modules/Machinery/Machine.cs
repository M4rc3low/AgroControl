namespace AgroControl.Domain.Modules.Machinery;

public sealed class Machine
{
    private Machine() { }

    private Machine(
        Guid id,
        Guid organizationId,
        Guid? farmId,
        string name,
        string internalCode,
        MachineKind kind,
        string? manufacturer,
        string? model,
        int? year,
        decimal currentHourMeter,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FarmId = farmId;
        Name = name;
        InternalCode = internalCode;
        Kind = kind;
        Manufacturer = manufacturer;
        Model = model;
        Year = year;
        Status = MachineStatus.Active;
        CurrentHourMeter = currentHourMeter;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? FarmId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string InternalCode { get; private set; } = string.Empty;
    public MachineKind Kind { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public int? Year { get; private set; }
    public MachineStatus Status { get; private set; }
    public decimal CurrentHourMeter { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Machine Create(
        Guid organizationId,
        Guid? farmId,
        string name,
        string internalCode,
        MachineKind kind,
        string? manufacturer,
        string? model,
        int? year,
        decimal initialHourMeter,
        DateTime nowUtc)
    {
        Validate(name, internalCode, year, initialHourMeter);
        return new Machine(
            Guid.NewGuid(), organizationId, farmId, name.Trim(), internalCode.Trim(), kind,
            Normalize(manufacturer), Normalize(model), year, initialHourMeter, nowUtc);
    }

    public void Update(
        Guid? farmId,
        string name,
        string internalCode,
        MachineKind kind,
        string? manufacturer,
        string? model,
        int? year,
        MachineStatus status,
        DateTime nowUtc)
    {
        Validate(name, internalCode, year, CurrentHourMeter);
        FarmId = farmId;
        Name = name.Trim();
        InternalCode = internalCode.Trim();
        Kind = kind;
        Manufacturer = Normalize(manufacturer);
        Model = Normalize(model);
        Year = year;
        Status = status;
        UpdatedAtUtc = nowUtc;
    }

    public void RecordHourMeter(decimal hours, DateTime nowUtc)
    {
        if (hours < CurrentHourMeter)
            throw new InvalidOperationException("Hour meter cannot regress.");
        if (hours < 0)
            throw new ArgumentOutOfRangeException(nameof(hours), "Hour meter cannot be negative.");

        CurrentHourMeter = hours;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        Status = MachineStatus.Inactive;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(string name, string internalCode, int? year, decimal hourMeter)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Machine name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(internalCode))
            throw new ArgumentException("Internal code is required.", nameof(internalCode));
        if (hourMeter < 0)
            throw new ArgumentOutOfRangeException(nameof(hourMeter), "Hour meter cannot be negative.");
        if (year is < 1900 or > 2200)
            throw new ArgumentOutOfRangeException(nameof(year), "Machine year is outside the supported range.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
