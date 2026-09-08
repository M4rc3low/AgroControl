using AgroControl.Application.Sustainability;
using AgroControl.Domain.Modules.Sustainability;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class SustainabilityRepository(AgroControlDbContext dbContext) : ISustainabilityRepository
{
    public async Task<(IReadOnlyList<EmissionFactor> Items, int TotalCount)> ListFactorsAsync(
        Guid organizationId, int skip, int take, string? search, EmissionSourceCategory? category,
        bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmissionFactors.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (category is not null) query = query.Where(x => x.Category == category.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.MethodologyReference, pattern));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<EmissionFactor?> GetFactorAsync(Guid organizationId, Guid factorId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<EmissionFactor> query = tracking ? dbContext.EmissionFactors : dbContext.EmissionFactors.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == factorId, cancellationToken);
    }

    public Task<bool> FactorNameExistsAsync(Guid organizationId, string name, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.EmissionFactors.AnyAsync(
            x => x.OrganizationId == organizationId && x.Name.ToLower() == name.ToLower() &&
                 (excludingId == null || x.Id != excludingId.Value), cancellationToken);

    public void AddFactor(EmissionFactor factor) => dbContext.EmissionFactors.Add(factor);

    public async Task<(IReadOnlyList<EmissionActivity> Items, int TotalCount)> ListActivitiesAsync(
        Guid organizationId, int skip, int take, DateOnly? from, DateOnly? to,
        EmissionSourceCategory? category, Guid? farmId, Guid? fieldId, Guid? seasonId,
        string? sourceModule, CancellationToken cancellationToken = default)
    {
        var query = BuildActivityQuery(organizationId, from, to, farmId, fieldId, seasonId);
        if (category is not null) query = query.Where(x => x.CategorySnapshot == category.Value);
        if (!string.IsNullOrWhiteSpace(sourceModule)) query = query.Where(x => x.SourceModule == sourceModule.Trim());
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ActivityDate).ThenByDescending(x => x.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> ExternalReferenceExistsAsync(
        Guid organizationId, string sourceModule, string sourceReferenceId, CancellationToken cancellationToken = default) =>
        dbContext.EmissionActivities.AsNoTracking().AnyAsync(
            x => x.OrganizationId == organizationId && x.SourceModule == sourceModule && x.SourceReferenceId == sourceReferenceId,
            cancellationToken);

    public async Task<SustainabilityAggregateProjection> GetAggregateAsync(
        Guid organizationId, DateOnly? from, DateOnly? to, Guid? farmId, Guid? fieldId, Guid? seasonId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildActivityQuery(organizationId, from, to, farmId, fieldId, seasonId);
        var activityCount = await query.CountAsync(cancellationToken);
        var estimatedActivityCount = await query.CountAsync(x => x.DataQuality == SustainabilityDataQuality.Estimated, cancellationToken);
        var totalKg = await query.SumAsync(x => (decimal?)x.EmissionsKgCo2e, cancellationToken) ?? 0m;
        var breakdown = await query
            .GroupBy(x => x.CategorySnapshot)
            .Select(group => new SustainabilityBreakdownProjection(
                group.Key,
                group.Count(),
                group.Sum(x => x.EmissionsKgCo2e)))
            .ToListAsync(cancellationToken);
        return new SustainabilityAggregateProjection(activityCount, estimatedActivityCount, totalKg, breakdown);
    }

    public void AddActivity(EmissionActivity activity) => dbContext.EmissionActivities.Add(activity);

    private IQueryable<EmissionActivity> BuildActivityQuery(
        Guid organizationId, DateOnly? from, DateOnly? to, Guid? farmId, Guid? fieldId, Guid? seasonId)
    {
        var query = dbContext.EmissionActivities.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (from is not null) query = query.Where(x => x.ActivityDate >= from.Value);
        if (to is not null) query = query.Where(x => x.ActivityDate <= to.Value);
        if (farmId is not null) query = query.Where(x => x.FarmId == farmId.Value);
        if (fieldId is not null) query = query.Where(x => x.FieldId == fieldId.Value);
        if (seasonId is not null) query = query.Where(x => x.SeasonId == seasonId.Value);
        return query;
    }
}
