namespace AgroControl.Domain.Modules.Irrigation;

public enum IrrigationMethod
{
    CenterPivot = 1,
    Drip = 2,
    Sprinkler = 3,
    MicroSprinkler = 4,
    Furrow = 5,
    Other = 6
}

public enum IrrigationApplicationSource
{
    Manual = 1,
    Meter = 2,
    Imported = 3,
    Correction = 4
}

public enum WaterCondition
{
    Critical = 1,
    Dry = 2,
    Target = 3,
    Wet = 4
}

public enum IrrigationRecommendation
{
    Irrigate = 1,
    Monitor = 2,
    AvoidIrrigation = 3
}

public sealed class IrrigationZone
{
    private IrrigationZone() { }

    private IrrigationZone(
        Guid id,
        Guid organizationId,
        Guid fieldId,
        string name,
        decimal areaHectares,
        IrrigationMethod method,
        decimal minimumMoisturePercent,
        decimal targetMoisturePercent,
        decimal maximumMoisturePercent,
        Guid? telemetryDeviceId,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FieldId = fieldId;
        Name = name;
        AreaHectares = areaHectares;
        Method = method;
        MinimumMoisturePercent = minimumMoisturePercent;
        TargetMoisturePercent = targetMoisturePercent;
        MaximumMoisturePercent = maximumMoisturePercent;
        TelemetryDeviceId = telemetryDeviceId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FieldId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal AreaHectares { get; private set; }
    public IrrigationMethod Method { get; private set; }
    public decimal MinimumMoisturePercent { get; private set; }
    public decimal TargetMoisturePercent { get; private set; }
    public decimal MaximumMoisturePercent { get; private set; }
    public Guid? TelemetryDeviceId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static IrrigationZone Create(
        Guid organizationId,
        Guid fieldId,
        string name,
        decimal areaHectares,
        IrrigationMethod method,
        decimal minimumMoisturePercent,
        decimal targetMoisturePercent,
        decimal maximumMoisturePercent,
        Guid? telemetryDeviceId,
        DateTime nowUtc)
    {
        Validate(fieldId, name, areaHectares, method, minimumMoisturePercent, targetMoisturePercent, maximumMoisturePercent);
        return new IrrigationZone(
            Guid.NewGuid(), organizationId, fieldId, name.Trim(), areaHectares, method,
            minimumMoisturePercent, targetMoisturePercent, maximumMoisturePercent, telemetryDeviceId, nowUtc);
    }

    public void Update(
        Guid fieldId,
        string name,
        decimal areaHectares,
        IrrigationMethod method,
        decimal minimumMoisturePercent,
        decimal targetMoisturePercent,
        decimal maximumMoisturePercent,
        Guid? telemetryDeviceId,
        DateTime nowUtc)
    {
        Validate(fieldId, name, areaHectares, method, minimumMoisturePercent, targetMoisturePercent, maximumMoisturePercent);
        FieldId = fieldId;
        Name = name.Trim();
        AreaHectares = areaHectares;
        Method = method;
        MinimumMoisturePercent = minimumMoisturePercent;
        TargetMoisturePercent = targetMoisturePercent;
        MaximumMoisturePercent = maximumMoisturePercent;
        TelemetryDeviceId = telemetryDeviceId;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(
        Guid fieldId,
        string name,
        decimal areaHectares,
        IrrigationMethod method,
        decimal minimumMoisturePercent,
        decimal targetMoisturePercent,
        decimal maximumMoisturePercent)
    {
        if (fieldId == Guid.Empty) throw new ArgumentException("Field id is required.", nameof(fieldId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Irrigation zone name is required.", nameof(name));
        if (areaHectares <= 0m) throw new ArgumentOutOfRangeException(nameof(areaHectares), "Zone area must be greater than zero.");
        if (!Enum.IsDefined(method)) throw new ArgumentOutOfRangeException(nameof(method), "Irrigation method is invalid.");
        if (minimumMoisturePercent < 0m || maximumMoisturePercent > 100m ||
            minimumMoisturePercent >= targetMoisturePercent || targetMoisturePercent >= maximumMoisturePercent)
            throw new ArgumentException("Moisture thresholds must satisfy 0 <= minimum < target < maximum <= 100.");
    }
}

public sealed class IrrigationApplication
{
    private IrrigationApplication() { }

    private IrrigationApplication(
        Guid id,
        Guid organizationId,
        Guid zoneId,
        Guid fieldId,
        decimal areaHectaresSnapshot,
        decimal depthMillimeters,
        IrrigationApplicationSource source,
        DateTime startedAtUtc,
        DateTime? endedAtUtc,
        string? notes,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ZoneId = zoneId;
        FieldId = fieldId;
        AreaHectaresSnapshot = areaHectaresSnapshot;
        DepthMillimeters = depthMillimeters;
        EstimatedVolumeCubicMeters = decimal.Round(depthMillimeters * areaHectaresSnapshot * 10m, 3, MidpointRounding.AwayFromZero);
        Source = source;
        StartedAtUtc = startedAtUtc;
        EndedAtUtc = endedAtUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ZoneId { get; private set; }
    public Guid FieldId { get; private set; }
    public decimal AreaHectaresSnapshot { get; private set; }
    public decimal DepthMillimeters { get; private set; }
    public decimal EstimatedVolumeCubicMeters { get; private set; }
    public IrrigationApplicationSource Source { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static IrrigationApplication Create(
        Guid organizationId,
        Guid zoneId,
        Guid fieldId,
        decimal areaHectaresSnapshot,
        decimal depthMillimeters,
        IrrigationApplicationSource source,
        DateTime startedAtUtc,
        DateTime? endedAtUtc,
        string? notes,
        DateTime createdAtUtc)
    {
        if (zoneId == Guid.Empty) throw new ArgumentException("Zone id is required.", nameof(zoneId));
        if (fieldId == Guid.Empty) throw new ArgumentException("Field id is required.", nameof(fieldId));
        if (areaHectaresSnapshot <= 0m) throw new ArgumentOutOfRangeException(nameof(areaHectaresSnapshot));
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        if (source == IrrigationApplicationSource.Correction)
        {
            if (depthMillimeters == 0m) throw new ArgumentOutOfRangeException(nameof(depthMillimeters), "Correction depth cannot be zero.");
        }
        else if (depthMillimeters <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(depthMillimeters), "Applied depth must be greater than zero.");
        }
        if (endedAtUtc is not null && endedAtUtc.Value < startedAtUtc)
            throw new ArgumentException("End time cannot be before start time.", nameof(endedAtUtc));

        return new IrrigationApplication(
            Guid.NewGuid(), organizationId, zoneId, fieldId, areaHectaresSnapshot, depthMillimeters,
            source, startedAtUtc, endedAtUtc, notes, createdAtUtc);
    }
}

public static class IrrigationDecisionPolicy
{
    public static WaterCondition Classify(
        double soilMoisturePercent,
        decimal minimumMoisturePercent,
        decimal targetMoisturePercent,
        decimal maximumMoisturePercent)
    {
        if (double.IsNaN(soilMoisturePercent) || double.IsInfinity(soilMoisturePercent) || soilMoisturePercent < 0d || soilMoisturePercent > 100d)
            throw new ArgumentOutOfRangeException(nameof(soilMoisturePercent), "Soil moisture must be between 0 and 100 percent.");

        var value = (decimal)soilMoisturePercent;
        if (value < minimumMoisturePercent) return WaterCondition.Critical;
        if (value < targetMoisturePercent) return WaterCondition.Dry;
        if (value <= maximumMoisturePercent) return WaterCondition.Target;
        return WaterCondition.Wet;
    }

    public static IrrigationRecommendation Recommend(WaterCondition condition) => condition switch
    {
        WaterCondition.Critical => IrrigationRecommendation.Irrigate,
        WaterCondition.Dry => IrrigationRecommendation.Irrigate,
        WaterCondition.Target => IrrigationRecommendation.Monitor,
        WaterCondition.Wet => IrrigationRecommendation.AvoidIrrigation,
        _ => IrrigationRecommendation.Monitor
    };
}
