namespace AgroControl.Domain.Modules.Finance;

public sealed class FinancialTransaction
{
    private FinancialTransaction() { }

    private FinancialTransaction(
        Guid id,
        Guid organizationId,
        Guid? categoryId,
        Guid? costCenterId,
        FinancialEntryType type,
        string description,
        string? counterparty,
        decimal amount,
        DateOnly competenceDate,
        DateOnly? dueDate,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? notes,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CategoryId = categoryId;
        CostCenterId = costCenterId;
        Type = type;
        Status = FinancialStatus.Pending;
        Description = description;
        Counterparty = counterparty;
        Amount = amount;
        CompetenceDate = competenceDate;
        DueDate = dueDate;
        FarmId = farmId;
        FieldId = fieldId;
        SeasonId = seasonId;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public FinancialEntryType Type { get; private set; }
    public FinancialStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Counterparty { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly CompetenceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateOnly? SettledOn { get; private set; }
    public Guid? FarmId { get; private set; }
    public Guid? FieldId { get; private set; }
    public Guid? SeasonId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static FinancialTransaction Create(
        Guid organizationId,
        Guid? categoryId,
        Guid? costCenterId,
        FinancialEntryType type,
        string description,
        string? counterparty,
        decimal amount,
        DateOnly competenceDate,
        DateOnly? dueDate,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? notes,
        DateTime nowUtc)
    {
        Validate(organizationId, description, amount);
        return new FinancialTransaction(
            Guid.NewGuid(), organizationId, categoryId, costCenterId, type,
            description.Trim(), Normalize(counterparty), amount, competenceDate, dueDate,
            farmId, fieldId, seasonId, Normalize(notes), nowUtc);
    }

    public void Update(
        Guid? categoryId,
        Guid? costCenterId,
        FinancialEntryType type,
        string description,
        string? counterparty,
        decimal amount,
        DateOnly competenceDate,
        DateOnly? dueDate,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? notes,
        DateTime nowUtc)
    {
        if (Status != FinancialStatus.Pending)
            throw new InvalidOperationException("Only pending financial transactions can be edited.");

        Validate(OrganizationId, description, amount);
        CategoryId = categoryId;
        CostCenterId = costCenterId;
        Type = type;
        Description = description.Trim();
        Counterparty = Normalize(counterparty);
        Amount = amount;
        CompetenceDate = competenceDate;
        DueDate = dueDate;
        FarmId = farmId;
        FieldId = fieldId;
        SeasonId = seasonId;
        Notes = Normalize(notes);
        UpdatedAtUtc = nowUtc;
    }

    public void Settle(DateOnly settledOn, DateTime nowUtc)
    {
        if (Status != FinancialStatus.Pending)
            throw new InvalidOperationException("Only pending financial transactions can be settled.");

        Status = Type == FinancialEntryType.Expense ? FinancialStatus.Paid : FinancialStatus.Received;
        SettledOn = settledOn;
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(DateTime nowUtc)
    {
        if (Status != FinancialStatus.Pending)
            throw new InvalidOperationException("Only pending financial transactions can be cancelled.");

        Status = FinancialStatus.Cancelled;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(Guid organizationId, string description, decimal amount)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
