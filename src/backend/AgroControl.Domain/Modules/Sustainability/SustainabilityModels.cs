namespace AgroControl.Domain.Modules.Sustainability;

public enum EmissionSourceCategory
{
    Fuel = 1,
    Fertilizer = 2,
    Energy = 3,
    Transport = 4,
    Custom = 5
}

public enum EmissionActivityOrigin
{
    Manual = 1,
    External = 2,
    Correction = 3
}

public enum SustainabilityDataQuality
{
    Measured = 1,
    Recorded = 2,
    Estimated = 3
}

public sealed class EmissionFactor
{
    private EmissionFactor() { }

    private EmissionFactor(
        Guid id,
        Guid organizationId,
        string name,
        EmissionSourceCategory category,
        string unit,
        decimal kgCo2ePerUnit,
        string methodologyReference,
        string? notes,
        DateOnly validFrom,
        DateOnly? validTo,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Apply(name, category, unit, kgCo2ePerUnit, methodologyReference, notes, validFrom, validTo, createdAtUtc);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public EmissionSourceCategory Category { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal KgCo2ePerUnit { get; private set; }
    public string MethodologyReference { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static EmissionFactor Create(
        Guid organizationId,
        string name,
        EmissionSourceCategory category,
        string unit,
        decimal kgCo2ePerUnit,
        string methodologyReference,
        string? notes,
        DateOnly validFrom,
        DateOnly? validTo,
        DateTime nowUtc)
    {
        return new EmissionFactor(
            Guid.NewGuid(), organizationId, name, category, unit, kgCo2ePerUnit,
            methodologyReference, notes, validFrom, validTo, nowUtc);
    }

    public void Update(
        string name,
        EmissionSourceCategory category,
        string unit,
        decimal kgCo2ePerUnit,
        string methodologyReference,
        string? notes,
        DateOnly validFrom,
        DateOnly? validTo,
        DateTime nowUtc)
    {
        Apply(name, category, unit, kgCo2ePerUnit, methodologyReference, notes, validFrom, validTo, nowUtc);
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    public bool IsValidOn(DateOnly date) => date >= ValidFrom && (ValidTo is null || date <= ValidTo.Value);

    private void Apply(
        string name,
        EmissionSourceCategory category,
        string unit,
        decimal kgCo2ePerUnit,
        string methodologyReference,
        string? notes,
        DateOnly validFrom,
        DateOnly? validTo,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Emission factor name is required.", nameof(name));
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category), "Emission source category is invalid.");
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Emission factor unit is required.", nameof(unit));
        if (kgCo2ePerUnit <= 0m) throw new ArgumentOutOfRangeException(nameof(kgCo2ePerUnit), "Emission factor must be greater than zero.");
        if (string.IsNullOrWhiteSpace(methodologyReference)) throw new ArgumentException("Methodology reference is required.", nameof(methodologyReference));
        if (validFrom == default) throw new ArgumentException("Valid-from date is required.", nameof(validFrom));
        if (validTo is not null && validTo.Value < validFrom) throw new ArgumentException("Valid-to date cannot be before valid-from date.", nameof(validTo));

        Name = name.Trim();
        Category = category;
        Unit = unit.Trim();
        KgCo2ePerUnit = kgCo2ePerUnit;
        MethodologyReference = methodologyReference.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ValidFrom = validFrom;
        ValidTo = validTo;
        UpdatedAtUtc = nowUtc;
    }
}

public sealed class EmissionActivity
{
    private EmissionActivity() { }

    private EmissionActivity(
        Guid id,
        Guid organizationId,
        Guid emissionFactorId,
        string factorNameSnapshot,
        EmissionSourceCategory categorySnapshot,
        string unitSnapshot,
        decimal factorKgCo2ePerUnitSnapshot,
        decimal quantity,
        DateOnly activityDate,
        EmissionActivityOrigin origin,
        SustainabilityDataQuality dataQuality,
        string description,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? sourceModule,
        string? sourceReferenceId,
        string? notes,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        EmissionFactorId = emissionFactorId;
        FactorNameSnapshot = factorNameSnapshot;
        CategorySnapshot = categorySnapshot;
        UnitSnapshot = unitSnapshot;
        FactorKgCo2ePerUnitSnapshot = factorKgCo2ePerUnitSnapshot;
        Quantity = quantity;
        EmissionsKgCo2e = decimal.Round(quantity * factorKgCo2ePerUnitSnapshot, 6, MidpointRounding.AwayFromZero);
        ActivityDate = activityDate;
        Origin = origin;
        DataQuality = dataQuality;
        Description = description.Trim();
        FarmId = farmId;
        FieldId = fieldId;
        SeasonId = seasonId;
        SourceModule = string.IsNullOrWhiteSpace(sourceModule) ? null : sourceModule.Trim();
        SourceReferenceId = string.IsNullOrWhiteSpace(sourceReferenceId) ? null : sourceReferenceId.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EmissionFactorId { get; private set; }
    public string FactorNameSnapshot { get; private set; } = string.Empty;
    public EmissionSourceCategory CategorySnapshot { get; private set; }
    public string UnitSnapshot { get; private set; } = string.Empty;
    public decimal FactorKgCo2ePerUnitSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal EmissionsKgCo2e { get; private set; }
    public decimal EmissionsTCo2e => decimal.Round(EmissionsKgCo2e / 1000m, 6, MidpointRounding.AwayFromZero);
    public DateOnly ActivityDate { get; private set; }
    public EmissionActivityOrigin Origin { get; private set; }
    public SustainabilityDataQuality DataQuality { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? FarmId { get; private set; }
    public Guid? FieldId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public string? SourceModule { get; private set; }
    public string? SourceReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static EmissionActivity Create(
        Guid organizationId,
        EmissionFactor factor,
        decimal quantity,
        DateOnly activityDate,
        EmissionActivityOrigin origin,
        SustainabilityDataQuality dataQuality,
        string description,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? sourceModule,
        string? sourceReferenceId,
        string? notes,
        DateTime createdAtUtc)
    {
        if (factor.OrganizationId != organizationId) throw new ArgumentException("Emission factor belongs to another organization.", nameof(factor));
        if (activityDate == default) throw new ArgumentException("Activity date is required.", nameof(activityDate));
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        if (!Enum.IsDefined(dataQuality)) throw new ArgumentOutOfRangeException(nameof(dataQuality));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Activity description is required.", nameof(description));
        if (origin == EmissionActivityOrigin.Correction)
        {
            if (quantity == 0m) throw new ArgumentOutOfRangeException(nameof(quantity), "Correction quantity cannot be zero.");
        }
        else if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Activity quantity must be greater than zero.");
        }

        var hasModule = !string.IsNullOrWhiteSpace(sourceModule);
        var hasReference = !string.IsNullOrWhiteSpace(sourceReferenceId);
        if (hasModule != hasReference) throw new ArgumentException("Source module and source reference id must be informed together.");
        if (origin == EmissionActivityOrigin.External && !hasModule)
            throw new ArgumentException("External activities require source module and source reference id.");
        if (origin == EmissionActivityOrigin.Manual && hasModule)
            throw new ArgumentException("Manual activities cannot carry an external source reference.");

        return new EmissionActivity(
            Guid.NewGuid(), organizationId, factor.Id, factor.Name, factor.Category, factor.Unit,
            factor.KgCo2ePerUnit, quantity, activityDate, origin, dataQuality, description,
            farmId, fieldId, seasonId, sourceModule, sourceReferenceId, notes, createdAtUtc);
    }
}
