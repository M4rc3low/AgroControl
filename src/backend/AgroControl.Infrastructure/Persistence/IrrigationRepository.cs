using AgroControl.Application.Irrigation;
using AgroControl.Domain.Modules.Irrigation;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class IrrigationRepository(AgroControlDbContext dbContext) : IIrrigationRepository
{
    public async Task<(IReadOnlyList<IrrigationZone> Items, int TotalCount)> ListZonesAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? fieldId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.IrrigationZones.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (fieldId is not null) query = query.Where(item => item.FieldId == fieldId.Value);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Name).ThenBy(item => item.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<IrrigationZone?> GetZoneAsync(Guid organizationId, Guid zoneId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<IrrigationZone> query = dbContext.IrrigationZones.Where(item => item.OrganizationId == organizationId && item.Id == zoneId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public void AddZone(IrrigationZone zone) => dbContext.IrrigationZones.Add(zone);

    public async Task<(IReadOnlyList<IrrigationApplication> Items, int TotalCount)> ListApplicationsAsync(
        Guid organizationId,
        int skip,
        int take,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyApplicationFilters(dbContext.IrrigationApplications.AsNoTracking(), organizationId, fieldId, zoneId, fromUtc, toUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.StartedAtUtc).ThenByDescending(item => item.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IrrigationSummaryProjection> GetSummaryAsync(
        Guid organizationId,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyApplicationFilters(dbContext.IrrigationApplications.AsNoTracking(), organizationId, fieldId, zoneId, fromUtc, toUtc);
        var count = await query.CountAsync(cancellationToken);
        if (count == 0) return new IrrigationSummaryProjection(0, 0m, 0m);
        var totalDepth = await query.SumAsync(item => item.DepthMillimeters, cancellationToken);
        var totalVolume = await query.SumAsync(item => item.EstimatedVolumeCubicMeters, cancellationToken);
        return new IrrigationSummaryProjection(count, totalDepth, totalVolume);
    }

    public void AddApplication(IrrigationApplication application) => dbContext.IrrigationApplications.Add(application);

    private static IQueryable<IrrigationApplication> ApplyApplicationFilters(
        IQueryable<IrrigationApplication> query,
        Guid organizationId,
        Guid? fieldId,
        Guid? zoneId,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        query = query.Where(item => item.OrganizationId == organizationId);
        if (fieldId is not null) query = query.Where(item => item.FieldId == fieldId.Value);
        if (zoneId is not null) query = query.Where(item => item.ZoneId == zoneId.Value);
        if (fromUtc is not null) query = query.Where(item => item.StartedAtUtc >= fromUtc.Value);
        if (toUtc is not null) query = query.Where(item => item.StartedAtUtc <= toUtc.Value);
        return query;
    }
}
