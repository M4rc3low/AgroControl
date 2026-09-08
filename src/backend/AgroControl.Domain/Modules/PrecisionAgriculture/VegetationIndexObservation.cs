namespace AgroControl.Domain.Modules.PrecisionAgriculture;

public enum VegetationIndexType
{
    NDVI,
    NDRE,
    EVI,
    Custom
}

public sealed class VegetationIndexObservation
{
    private VegetationIndexObservation() { }

    private VegetationIndexObservation(Guid id, Guid organizationId, Guid sceneId, Guid fieldId, Guid? seasonId,
        Guid? managementZoneId, VegetationIndexType indexType, string? customIndexName, decimal minimum, decimal maximum,
        decimal mean, decimal median, decimal standardDeviation, decimal validCoveragePercent, long? sampleCount,
        string source, DateTime observedAtUtc, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        SceneId = sceneId;
        FieldId = fieldId;
        SeasonId = seasonId;
        ManagementZoneId = managementZoneId;
        IndexType = indexType;
        CustomIndexName = customIndexName;
        Minimum = minimum;
        Maximum = maximum;
        Mean = mean;
        Median = median;
        StandardDeviation = standardDeviation;
        ValidCoveragePercent = validCoveragePercent;
        SampleCount = sampleCount;
        Source = source;
        ObservedAtUtc = NormalizeUtc(observedAtUtc);
        CreatedAtUtc = NormalizeUtc(createdAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid SceneId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public Guid? ManagementZoneId { get; private set; }
    public VegetationIndexType IndexType { get; private set; }
    public string? CustomIndexName { get; private set; }
    public decimal Minimum { get; private set; }
    public decimal Maximum { get; private set; }
    public decimal Mean { get; private set; }
    public decimal Median { get; private set; }
    public decimal StandardDeviation { get; private set; }
    public decimal ValidCoveragePercent { get; private set; }
    public long? SampleCount { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public DateTime ObservedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static VegetationIndexObservation Create(Guid organizationId, Guid sceneId, Guid fieldId, Guid? seasonId,
        Guid? managementZoneId, VegetationIndexType indexType, string? customIndexName, decimal minimum, decimal maximum,
        decimal mean, decimal median, decimal standardDeviation, decimal validCoveragePercent, long? sampleCount,
        string source, DateTime observedAtUtc, DateTime createdAtUtc)
    {
        Validate(organizationId, sceneId, fieldId, indexType, customIndexName, minimum, maximum, mean, median,
            standardDeviation, validCoveragePercent, sampleCount, source);
        return new VegetationIndexObservation(Guid.NewGuid(), organizationId, sceneId, fieldId, seasonId, managementZoneId,
            indexType, Clean(customIndexName), minimum, maximum, mean, median, standardDeviation, validCoveragePercent,
            sampleCount, source.Trim(), observedAtUtc, createdAtUtc);
    }

    private static void Validate(Guid organizationId, Guid sceneId, Guid fieldId, VegetationIndexType indexType,
        string? customIndexName, decimal minimum, decimal maximum, decimal mean, decimal median,
        decimal standardDeviation, decimal validCoveragePercent, long? sampleCount, string source)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (sceneId == Guid.Empty) throw new ArgumentException("Scene id is required.", nameof(sceneId));
        if (fieldId == Guid.Empty) throw new ArgumentException("Field id is required.", nameof(fieldId));
        if (!Enum.IsDefined(indexType)) throw new ArgumentOutOfRangeException(nameof(indexType));
        if (indexType == VegetationIndexType.Custom && string.IsNullOrWhiteSpace(customIndexName))
            throw new ArgumentException("Custom index name is required for Custom observations.", nameof(customIndexName));
        if (customIndexName?.Trim().Length > 120) throw new ArgumentException("Custom index name cannot exceed 120 characters.", nameof(customIndexName));
        if (minimum > maximum) throw new ArgumentException("Minimum cannot exceed maximum.", nameof(minimum));
        if (mean < minimum || mean > maximum) throw new ArgumentOutOfRangeException(nameof(mean), "Mean must be between minimum and maximum.");
        if (median < minimum || median > maximum) throw new ArgumentOutOfRangeException(nameof(median), "Median must be between minimum and maximum.");
        if (standardDeviation < 0m) throw new ArgumentOutOfRangeException(nameof(standardDeviation), "Standard deviation cannot be negative.");
        if (validCoveragePercent is < 0m or > 100m) throw new ArgumentOutOfRangeException(nameof(validCoveragePercent), "Valid coverage must be between 0 and 100 percent.");
        if (sampleCount < 0) throw new ArgumentOutOfRangeException(nameof(sampleCount), "Sample count cannot be negative.");
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Observation source is required.", nameof(source));
        if (source.Trim().Length > 120) throw new ArgumentException("Observation source cannot exceed 120 characters.", nameof(source));

        if (indexType is VegetationIndexType.NDVI or VegetationIndexType.NDRE or VegetationIndexType.EVI)
        {
            ValidateStandardRange(minimum, nameof(minimum));
            ValidateStandardRange(maximum, nameof(maximum));
            ValidateStandardRange(mean, nameof(mean));
            ValidateStandardRange(median, nameof(median));
        }
    }

    private static void ValidateStandardRange(decimal value, string parameter)
    {
        if (value is < -1m or > 1m)
            throw new ArgumentOutOfRangeException(parameter, "NDVI, NDRE and EVI values must remain between -1 and 1 in the normalized AgroControl contract.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
