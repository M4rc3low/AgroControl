using AgroControl.Domain.Modules.Sustainability;

namespace AgroControl.Application.Sustainability;

public interface ISustainabilityRepository
{
    Task<(IReadOnlyList<EmissionFactor> Items, int TotalCount)> ListFactorsAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        EmissionSourceCategory? category,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<EmissionFactor?> GetFactorAsync(Guid organizationId, Guid factorId, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> FactorNameExistsAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddFactor(EmissionFactor factor);

    Task<(IReadOnlyList<EmissionActivity> Items, int TotalCount)> ListActivitiesAsync(
        Guid organizationId,
        int skip,
        int take,
        DateOnly? from,
        DateOnly? to,
        EmissionSourceCategory? category,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        string? sourceModule,
        CancellationToken cancellationToken = default);

    Task<bool> ExternalReferenceExistsAsync(
        Guid organizationId,
        string sourceModule,
        string sourceReferenceId,
        CancellationToken cancellationToken = default);

    Task<SustainabilityAggregateProjection> GetAggregateAsync(
        Guid organizationId,
        DateOnly? from,
        DateOnly? to,
        Guid? farmId,
        Guid? fieldId,
        Guid? seasonId,
        CancellationToken cancellationToken = default);

    void AddActivity(EmissionActivity activity);
}
