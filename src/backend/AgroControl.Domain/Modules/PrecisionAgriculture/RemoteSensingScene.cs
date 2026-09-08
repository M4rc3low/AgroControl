namespace AgroControl.Domain.Modules.PrecisionAgriculture;

public enum RemoteSensingPlatform
{
    Satellite,
    Drone,
    Other
}

public sealed class RemoteSensingScene
{
    private RemoteSensingScene() { }

    private RemoteSensingScene(Guid id, Guid organizationId, Guid fieldId, Guid? seasonId, string provider,
        string externalId, RemoteSensingPlatform platform, DateTime acquiredAtUtc, decimal? cloudCoveragePercent,
        decimal? spatialResolutionMeters, string? assetReference, string? notes, DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FieldId = fieldId;
        SeasonId = seasonId;
        Provider = provider;
        ExternalId = externalId;
        Platform = platform;
        AcquiredAtUtc = NormalizeUtc(acquiredAtUtc);
        CloudCoveragePercent = cloudCoveragePercent;
        SpatialResolutionMeters = spatialResolutionMeters;
        AssetReference = assetReference;
        Notes = notes;
        IsActive = true;
        CreatedAtUtc = NormalizeUtc(nowUtc);
        UpdatedAtUtc = NormalizeUtc(nowUtc);
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
    public RemoteSensingPlatform Platform { get; private set; }
    public DateTime AcquiredAtUtc { get; private set; }
    public decimal? CloudCoveragePercent { get; private set; }
    public decimal? SpatialResolutionMeters { get; private set; }
    public string? AssetReference { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static RemoteSensingScene Create(Guid organizationId, Guid fieldId, Guid? seasonId, string provider,
        string externalId, RemoteSensingPlatform platform, DateTime acquiredAtUtc, decimal? cloudCoveragePercent,
        decimal? spatialResolutionMeters, string? assetReference, string? notes, DateTime nowUtc)
    {
        Validate(organizationId, fieldId, provider, externalId, platform, cloudCoveragePercent, spatialResolutionMeters,
            assetReference, notes);
        return new RemoteSensingScene(Guid.NewGuid(), organizationId, fieldId, seasonId, provider.Trim(), externalId.Trim(),
            platform, acquiredAtUtc, cloudCoveragePercent, spatialResolutionMeters, Clean(assetReference), Clean(notes), nowUtc);
    }

    public void UpdateContext(Guid fieldId, Guid? seasonId, RemoteSensingPlatform platform, DateTime acquiredAtUtc,
        decimal? cloudCoveragePercent, decimal? spatialResolutionMeters, string? assetReference, string? notes, DateTime nowUtc)
    {
        Validate(OrganizationId, fieldId, Provider, ExternalId, platform, cloudCoveragePercent, spatialResolutionMeters,
            assetReference, notes);
        FieldId = fieldId;
        SeasonId = seasonId;
        Platform = platform;
        AcquiredAtUtc = NormalizeUtc(acquiredAtUtc);
        CloudCoveragePercent = cloudCoveragePercent;
        SpatialResolutionMeters = spatialResolutionMeters;
        AssetReference = Clean(assetReference);
        Notes = Clean(notes);
        UpdatedAtUtc = NormalizeUtc(nowUtc);
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = NormalizeUtc(nowUtc);
    }

    private static void Validate(Guid organizationId, Guid fieldId, string provider, string externalId,
        RemoteSensingPlatform platform, decimal? cloudCoveragePercent, decimal? spatialResolutionMeters,
        string? assetReference, string? notes)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (fieldId == Guid.Empty) throw new ArgumentException("Field id is required.", nameof(fieldId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));
        if (provider.Trim().Length > 120) throw new ArgumentException("Provider cannot exceed 120 characters.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalId)) throw new ArgumentException("External id is required.", nameof(externalId));
        if (externalId.Trim().Length > 200) throw new ArgumentException("External id cannot exceed 200 characters.", nameof(externalId));
        if (!Enum.IsDefined(platform)) throw new ArgumentOutOfRangeException(nameof(platform));
        if (cloudCoveragePercent is < 0m or > 100m) throw new ArgumentOutOfRangeException(nameof(cloudCoveragePercent), "Cloud coverage must be between 0 and 100 percent.");
        if (spatialResolutionMeters is <= 0m) throw new ArgumentOutOfRangeException(nameof(spatialResolutionMeters), "Spatial resolution must be greater than zero.");
        if (assetReference?.Trim().Length > 2048) throw new ArgumentException("Asset reference cannot exceed 2048 characters.", nameof(assetReference));
        if (notes?.Trim().Length > 2000) throw new ArgumentException("Notes cannot exceed 2000 characters.", nameof(notes));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
