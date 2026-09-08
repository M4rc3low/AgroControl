namespace AgroControl.Domain.Modules.Farms;

public sealed class Farm
{
    private static readonly HashSet<string> BrazilianStateCodes = new(StringComparer.Ordinal)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    private Farm() { }

    private Farm(
        Guid id,
        Guid organizationId,
        string name,
        decimal totalAreaHectares,
        string? city,
        string? state,
        DateTime createdAtUtc,
        Guid? operationalRegionId,
        string countryCode,
        string? stateCode,
        string? municipalityCode,
        string? postalCode,
        decimal? latitude,
        decimal? longitude,
        string timeZoneId)
    {
        Id = id;
        OrganizationId = organizationId;
        Apply(
            name,
            totalAreaHectares,
            city,
            state,
            operationalRegionId,
            countryCode,
            stateCode,
            municipalityCode,
            postalCode,
            latitude,
            longitude,
            timeZoneId,
            createdAtUtc);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? OperationalRegionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TotalAreaHectares { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string CountryCode { get; private set; } = "BR";
    public string? StateCode { get; private set; }
    public string? MunicipalityCode { get; private set; }
    public string? PostalCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string TimeZoneId { get; private set; } = "America/Sao_Paulo";
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Farm Create(
        Guid organizationId,
        string name,
        decimal totalAreaHectares,
        string? city,
        string? state,
        DateTime nowUtc,
        Guid? operationalRegionId = null,
        string countryCode = "BR",
        string? stateCode = null,
        string? municipalityCode = null,
        string? postalCode = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? timeZoneId = null)
    {
        var normalizedState = NormalizeStateCode(stateCode ?? state);
        var normalizedCountry = NormalizeCountryCode(countryCode);
        var normalizedTimeZone = NormalizeTimeZone(timeZoneId);
        Validate(name, totalAreaHectares, normalizedCountry, normalizedState, latitude, longitude, normalizedTimeZone);

        return new Farm(
            Guid.NewGuid(),
            organizationId,
            name.Trim(),
            totalAreaHectares,
            Normalize(city),
            normalizedState,
            nowUtc,
            operationalRegionId,
            normalizedCountry,
            normalizedState,
            Normalize(municipalityCode),
            Normalize(postalCode),
            latitude,
            longitude,
            normalizedTimeZone);
    }

    public void Update(
        string name,
        decimal totalAreaHectares,
        string? city,
        string? state,
        DateTime nowUtc,
        Guid? operationalRegionId = null,
        string countryCode = "BR",
        string? stateCode = null,
        string? municipalityCode = null,
        string? postalCode = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? timeZoneId = null)
    {
        Apply(
            name,
            totalAreaHectares,
            city,
            state,
            operationalRegionId,
            countryCode,
            stateCode,
            municipalityCode,
            postalCode,
            latitude,
            longitude,
            timeZoneId ?? TimeZoneId,
            nowUtc);
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private void Apply(
        string name,
        decimal totalAreaHectares,
        string? city,
        string? state,
        Guid? operationalRegionId,
        string countryCode,
        string? stateCode,
        string? municipalityCode,
        string? postalCode,
        decimal? latitude,
        decimal? longitude,
        string? timeZoneId,
        DateTime nowUtc)
    {
        var normalizedState = NormalizeStateCode(stateCode ?? state);
        var normalizedCountry = NormalizeCountryCode(countryCode);
        var normalizedTimeZone = NormalizeTimeZone(timeZoneId);
        Validate(name, totalAreaHectares, normalizedCountry, normalizedState, latitude, longitude, normalizedTimeZone);

        Name = name.Trim();
        TotalAreaHectares = totalAreaHectares;
        City = Normalize(city);
        State = normalizedState;
        OperationalRegionId = operationalRegionId;
        CountryCode = normalizedCountry;
        StateCode = normalizedState;
        MunicipalityCode = Normalize(municipalityCode);
        PostalCode = Normalize(postalCode);
        Latitude = latitude;
        Longitude = longitude;
        TimeZoneId = normalizedTimeZone;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(
        string name,
        decimal area,
        string countryCode,
        string? stateCode,
        decimal? latitude,
        decimal? longitude,
        string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Farm name is required.", nameof(name));
        if (area <= 0)
            throw new ArgumentOutOfRangeException(nameof(area), "Farm area must be greater than zero.");
        if (countryCode.Length != 2 || !countryCode.All(char.IsLetter))
            throw new ArgumentException("Country code must use ISO 3166-1 alpha-2 format.", nameof(countryCode));
        if (countryCode == "BR" && stateCode is not null && !BrazilianStateCodes.Contains(stateCode))
            throw new ArgumentException("State code must be a valid Brazilian UF when countryCode is BR.", nameof(stateCode));
        if (latitude is < -90m or > 90m)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude is < -180m or > 180m)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException("Time zone id must be a valid IANA/system time zone.", nameof(timeZoneId));
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException("Time zone id is invalid.", nameof(timeZoneId));
        }
    }

    private static string NormalizeCountryCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "BR" : value.Trim().ToUpperInvariant();

    private static string? NormalizeStateCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string NormalizeTimeZone(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "America/Sao_Paulo" : value.Trim();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
