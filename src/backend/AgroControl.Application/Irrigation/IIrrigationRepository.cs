using AgroControl.Domain.Modules.Irrigation;

namespace AgroControl.Application.Irrigation;

public interface IIrrigationRepository
{
    Task<(IReadOnlyList<IrrigationZone> Items, int TotalCount)> ListZonesAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? fieldId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IrrigationZone?> GetZoneAsync(Guid organizationId, Guid zoneId, bool tracking, CancellationToken cancellationToken = default);
    void AddZone(IrrigationZone zone);

    Task<(IReadOnlyList<IrrigationApplication> Items, int TotalCount)> ListApplicationsAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IrrigationSummaryProjection> GetSummaryAsync(
        Guid organizationId,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    void AddApplication(IrrigationApplication application);
}
