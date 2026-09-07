namespace AgroControl.Domain.Modules.Seasons;

public sealed class Season
{
    private Season() { }

    private Season(
        Guid id,
        Guid organizationId,
        Guid fieldId,
        Guid cropId,
        string name,
        DateOnly startDate,
        DateOnly? endDate,
        decimal? expectedYieldPerHectare,
        DateTime createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FieldId = fieldId;
        CropId = cropId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        ExpectedYieldPerHectare = expectedYieldPerHectare;
        Status = SeasonStatus.Planned;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid CropId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public decimal? ExpectedYieldPerHectare { get; private set; }
    public decimal? ActualYieldPerHectare { get; private set; }
    public SeasonStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Season Create(
        Guid organizationId,
        Guid fieldId,
        Guid cropId,
        string name,
        DateOnly startDate,
        DateOnly? endDate,
        decimal? expectedYieldPerHectare,
        DateTime nowUtc)
    {
        Validate(fieldId, cropId, name, startDate, endDate, expectedYieldPerHectare, null);
        return new Season(
            Guid.NewGuid(),
            organizationId,
            fieldId,
            cropId,
            name.Trim(),
            startDate,
            endDate,
            expectedYieldPerHectare,
            nowUtc);
    }

    public void Update(
        Guid fieldId,
        Guid cropId,
        string name,
        DateOnly startDate,
        DateOnly? endDate,
        decimal? expectedYieldPerHectare,
        decimal? actualYieldPerHectare,
        SeasonStatus status,
        DateTime nowUtc)
    {
        Validate(fieldId, cropId, name, startDate, endDate, expectedYieldPerHectare, actualYieldPerHectare);
        FieldId = fieldId;
        CropId = cropId;
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        ExpectedYieldPerHectare = expectedYieldPerHectare;
        ActualYieldPerHectare = actualYieldPerHectare;
        Status = status;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    private static void Validate(
        Guid fieldId,
        Guid cropId,
        string name,
        DateOnly startDate,
        DateOnly? endDate,
        decimal? expectedYield,
        decimal? actualYield)
    {
        if (fieldId == Guid.Empty)
            throw new ArgumentException("Field id is required.", nameof(fieldId));
        if (cropId == Guid.Empty)
            throw new ArgumentException("Crop id is required.", nameof(cropId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Season name is required.", nameof(name));
        if (endDate is not null && endDate < startDate)
            throw new ArgumentException("Season end date cannot be before start date.", nameof(endDate));
        if (expectedYield is < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedYield));
        if (actualYield is < 0)
            throw new ArgumentOutOfRangeException(nameof(actualYield));
    }
}
