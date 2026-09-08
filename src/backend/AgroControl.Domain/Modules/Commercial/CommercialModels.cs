namespace AgroControl.Domain.Modules.Commercial;

public enum CustomerStatus
{
    Lead = 1,
    Prospect = 2,
    Customer = 3,
    Inactive = 4
}

public enum OpportunityStage
{
    Lead = 1,
    Qualification = 2,
    Proposal = 3,
    Negotiation = 4,
    Won = 5,
    Lost = 6
}

public sealed class CommercialCustomer
{
    private CommercialCustomer() { }

    private CommercialCustomer(Guid id, Guid organizationId, string name, string? tradeName, string? taxId,
        string? email, string? phone, string? countryCode, string? city, string? state, CustomerStatus status,
        string? notes, DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CreatedAtUtc = nowUtc;
        Apply(name, tradeName, taxId, email, phone, countryCode, city, state, status, notes, nowUtc);
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string? TaxId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? CountryCode { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public CustomerStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public bool IsInactive => Status == CustomerStatus.Inactive;

    public static CommercialCustomer Create(Guid organizationId, string name, string? tradeName, string? taxId,
        string? email, string? phone, string? countryCode, string? city, string? state, CustomerStatus status,
        string? notes, DateTime nowUtc) =>
        new(Guid.NewGuid(), organizationId, name, tradeName, taxId, email, phone, countryCode, city, state, status, notes, nowUtc);

    public void Update(string name, string? tradeName, string? taxId, string? email, string? phone,
        string? countryCode, string? city, string? state, CustomerStatus status, string? notes, DateTime nowUtc) =>
        Apply(name, tradeName, taxId, email, phone, countryCode, city, state, status, notes, nowUtc);

    private void Apply(string name, string? tradeName, string? taxId, string? email, string? phone,
        string? countryCode, string? city, string? state, CustomerStatus status, string? notes, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Customer name is required.", nameof(name));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) throw new ArgumentException("Customer email is invalid.", nameof(email));
        if (!string.IsNullOrWhiteSpace(countryCode) && (countryCode.Trim().Length != 2 || !countryCode.Trim().All(char.IsLetter)))
            throw new ArgumentException("Country code must use ISO alpha-2 format.", nameof(countryCode));

        Name = name.Trim();
        TradeName = Normalize(tradeName);
        TaxId = Normalize(taxId);
        Email = Normalize(email)?.ToLowerInvariant();
        Phone = Normalize(phone);
        CountryCode = Normalize(countryCode)?.ToUpperInvariant();
        City = Normalize(city);
        State = Normalize(state)?.ToUpperInvariant();
        Status = status;
        Notes = Normalize(notes);
        UpdatedAtUtc = nowUtc;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CommercialContact
{
    private CommercialContact() { }

    private CommercialContact(Guid id, Guid organizationId, Guid customerId, string name, string? role,
        string? email, string? phone, bool isPrimary, DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        CreatedAtUtc = nowUtc;
        IsActive = true;
        Apply(name, role, email, phone, isPrimary, nowUtc);
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Role { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static CommercialContact Create(Guid organizationId, Guid customerId, string name, string? role,
        string? email, string? phone, bool isPrimary, DateTime nowUtc)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        return new CommercialContact(Guid.NewGuid(), organizationId, customerId, name, role, email, phone, isPrimary, nowUtc);
    }

    public void Update(string name, string? role, string? email, string? phone, bool isPrimary, DateTime nowUtc)
    {
        if (!IsActive) throw new InvalidOperationException("Inactive contacts cannot be edited.");
        Apply(name, role, email, phone, isPrimary, nowUtc);
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        IsPrimary = false;
        UpdatedAtUtc = nowUtc;
    }

    private void Apply(string name, string? role, string? email, string? phone, bool isPrimary, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Contact name is required.", nameof(name));
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) throw new ArgumentException("Contact email is invalid.", nameof(email));
        Name = name.Trim();
        Role = Normalize(role);
        Email = Normalize(email)?.ToLowerInvariant();
        Phone = Normalize(phone);
        IsPrimary = isPrimary;
        UpdatedAtUtc = nowUtc;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CommercialOpportunity
{
    private CommercialOpportunity() { }

    private CommercialOpportunity(Guid id, Guid organizationId, Guid customerId, string title, Guid? farmId,
        Guid? cropId, Guid? seasonId, Guid? exportOrderId, decimal expectedValue, string currency,
        decimal probabilityPercent, DateOnly? expectedCloseDate, string? ownerName, string? nextStep,
        string? notes, DateTime nowUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        Stage = OpportunityStage.Lead;
        CreatedAtUtc = nowUtc;
        Apply(title, farmId, cropId, seasonId, exportOrderId, expectedValue, currency, probabilityPercent,
            expectedCloseDate, ownerName, nextStep, notes, nowUtc);
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid? FarmId { get; private set; }
    public Guid? CropId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public Guid? ExportOrderId { get; private set; }
    public decimal ExpectedValue { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public decimal ProbabilityPercent { get; private set; }
    public OpportunityStage Stage { get; private set; }
    public DateOnly? ExpectedCloseDate { get; private set; }
    public DateOnly? ClosedOn { get; private set; }
    public string? OwnerName { get; private set; }
    public string? NextStep { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public decimal WeightedValue => decimal.Round(ExpectedValue * ProbabilityPercent / 100m, 2, MidpointRounding.AwayFromZero);
    public bool IsTerminal => Stage is OpportunityStage.Won or OpportunityStage.Lost;

    public static CommercialOpportunity Create(Guid organizationId, Guid customerId, string title, Guid? farmId,
        Guid? cropId, Guid? seasonId, Guid? exportOrderId, decimal expectedValue, string currency,
        decimal probabilityPercent, DateOnly? expectedCloseDate, string? ownerName, string? nextStep,
        string? notes, DateTime nowUtc)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        return new CommercialOpportunity(Guid.NewGuid(), organizationId, customerId, title, farmId, cropId,
            seasonId, exportOrderId, expectedValue, currency, probabilityPercent, expectedCloseDate,
            ownerName, nextStep, notes, nowUtc);
    }

    public void Update(string title, Guid? farmId, Guid? cropId, Guid? seasonId, Guid? exportOrderId,
        decimal expectedValue, string currency, decimal probabilityPercent, DateOnly? expectedCloseDate,
        string? ownerName, string? nextStep, string? notes, DateTime nowUtc)
    {
        if (IsTerminal) throw new InvalidOperationException("Won or lost opportunities cannot be edited.");
        Apply(title, farmId, cropId, seasonId, exportOrderId, expectedValue, currency, probabilityPercent,
            expectedCloseDate, ownerName, nextStep, notes, nowUtc);
    }

    public void TransitionTo(OpportunityStage nextStage, DateOnly occurredOn, DateTime nowUtc)
    {
        if (occurredOn == default) throw new ArgumentException("Transition date is required.", nameof(occurredOn));
        if (Stage == nextStage) return;
        if (!CanTransition(Stage, nextStage))
            throw new InvalidOperationException($"Invalid commercial opportunity transition from {Stage} to {nextStage}.");
        Stage = nextStage;
        if (nextStage == OpportunityStage.Won)
        {
            ProbabilityPercent = 100m;
            ClosedOn = occurredOn;
        }
        else if (nextStage == OpportunityStage.Lost)
        {
            ProbabilityPercent = 0m;
            ClosedOn = occurredOn;
        }
        UpdatedAtUtc = nowUtc;
    }

    public static bool CanTransition(OpportunityStage current, OpportunityStage next) => current switch
    {
        OpportunityStage.Lead => next is OpportunityStage.Qualification or OpportunityStage.Lost,
        OpportunityStage.Qualification => next is OpportunityStage.Proposal or OpportunityStage.Lost,
        OpportunityStage.Proposal => next is OpportunityStage.Negotiation or OpportunityStage.Won or OpportunityStage.Lost,
        OpportunityStage.Negotiation => next is OpportunityStage.Proposal or OpportunityStage.Won or OpportunityStage.Lost,
        _ => false
    };

    private void Apply(string title, Guid? farmId, Guid? cropId, Guid? seasonId, Guid? exportOrderId,
        decimal expectedValue, string currency, decimal probabilityPercent, DateOnly? expectedCloseDate,
        string? ownerName, string? nextStep, string? notes, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Opportunity title is required.", nameof(title));
        if (expectedValue <= 0m) throw new ArgumentOutOfRangeException(nameof(expectedValue), "Expected value must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3 || !currency.Trim().All(char.IsLetter))
            throw new ArgumentException("Currency must use ISO 4217 three-letter format.", nameof(currency));
        if (probabilityPercent < 0m || probabilityPercent > 100m)
            throw new ArgumentOutOfRangeException(nameof(probabilityPercent), "Probability must be between 0 and 100.");
        Title = title.Trim();
        FarmId = farmId;
        CropId = cropId;
        SeasonId = seasonId;
        ExportOrderId = exportOrderId;
        ExpectedValue = expectedValue;
        Currency = currency.Trim().ToUpperInvariant();
        ProbabilityPercent = probabilityPercent;
        ExpectedCloseDate = expectedCloseDate;
        OwnerName = Normalize(ownerName);
        NextStep = Normalize(nextStep);
        Notes = Normalize(notes);
        UpdatedAtUtc = nowUtc;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class OpportunityStageEvent
{
    private OpportunityStageEvent() { }

    private OpportunityStageEvent(Guid id, Guid organizationId, Guid opportunityId, OpportunityStage? fromStage,
        OpportunityStage toStage, DateOnly occurredOn, string? notes, DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        OpportunityId = opportunityId;
        FromStage = fromStage;
        ToStage = toStage;
        OccurredOn = occurredOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public OpportunityStage? FromStage { get; private set; }
    public OpportunityStage ToStage { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static OpportunityStageEvent Create(Guid organizationId, Guid opportunityId, OpportunityStage? fromStage,
        OpportunityStage toStage, DateOnly occurredOn, string? notes, DateTime nowUtc)
    {
        if (opportunityId == Guid.Empty) throw new ArgumentException("Opportunity id is required.", nameof(opportunityId));
        if (occurredOn == default) throw new ArgumentException("Transition date is required.", nameof(occurredOn));
        if (!Enum.IsDefined(toStage)) throw new ArgumentOutOfRangeException(nameof(toStage));
        return new OpportunityStageEvent(Guid.NewGuid(), organizationId, opportunityId, fromStage, toStage, occurredOn, notes, nowUtc);
    }
}
