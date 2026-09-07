namespace AgroControl.Domain.Modules.Market;

public sealed class Commodity
{
    private Commodity() { }

    private Commodity(Guid id, Guid organizationId, string name, string symbol, string defaultCurrency, string defaultUnit, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        Symbol = symbol;
        DefaultCurrency = defaultCurrency;
        DefaultUnit = defaultUnit;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public string DefaultCurrency { get; private set; } = string.Empty;
    public string DefaultUnit { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Commodity Create(Guid organizationId, string name, string symbol, string defaultCurrency, string defaultUnit, DateTime nowUtc)
    {
        Validate(name, symbol, defaultCurrency, defaultUnit);
        return new Commodity(Guid.NewGuid(), organizationId, name.Trim(), symbol.Trim().ToUpperInvariant(), defaultCurrency.Trim().ToUpperInvariant(), defaultUnit.Trim(), nowUtc);
    }

    public void Update(string name, string symbol, string defaultCurrency, string defaultUnit, DateTime nowUtc)
    {
        Validate(name, symbol, defaultCurrency, defaultUnit);
        Name = name.Trim();
        Symbol = symbol.Trim().ToUpperInvariant();
        DefaultCurrency = defaultCurrency.Trim().ToUpperInvariant();
        DefaultUnit = defaultUnit.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(string name, string symbol, string currency, string unit)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Commodity name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Commodity symbol is required.", nameof(symbol));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Default currency is required.", nameof(currency));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Default unit is required.", nameof(unit));
    }
}
