using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Operations;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class MultiFarmRepository(AgroControlDbContext dbContext) : IMultiFarmRepository
{
    public async Task<(IReadOnlyList<OperationalRegion> Items, int TotalCount)> ListRegionsAsync(
        Guid organizationId,
        int skip,
        int take,
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.OperationalRegions.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(term) || item.Code.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<OperationalRegion?> GetRegionAsync(
        Guid organizationId,
        Guid regionId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OperationalRegion> query = dbContext.OperationalRegions
            .Where(item => item.OrganizationId == organizationId && item.Id == regionId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> RegionCodeExistsAsync(
        Guid organizationId,
        string normalizedCode,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.OperationalRegions.AsNoTracking().AnyAsync(item =>
            item.OrganizationId == organizationId &&
            item.Code == normalizedCode &&
            (excludingId == null || item.Id != excludingId.Value), cancellationToken);

    public void AddRegion(OperationalRegion region) => dbContext.OperationalRegions.Add(region);

    public async Task<IReadOnlyList<FarmAccessAssignment>> ListAssignmentsAsync(
        Guid organizationId,
        Guid userId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FarmAccessAssignments.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.UserId == userId);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        return await query.OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public Task<FarmAccessAssignment?> GetAssignmentAsync(
        Guid organizationId,
        Guid assignmentId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<FarmAccessAssignment> query = dbContext.FarmAccessAssignments
            .Where(item => item.OrganizationId == organizationId && item.Id == assignmentId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AssignmentExistsAsync(
        Guid organizationId,
        Guid userId,
        string scopeKey,
        CancellationToken cancellationToken = default) =>
        dbContext.FarmAccessAssignments.AsNoTracking().AnyAsync(item =>
            item.OrganizationId == organizationId &&
            item.UserId == userId &&
            item.ScopeKey == scopeKey &&
            item.IsActive,
            cancellationToken);

    public Task<bool> UserBelongsToOrganizationAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.OrganizationMemberships.AsNoTracking().AnyAsync(item =>
            item.OrganizationId == organizationId && item.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListFarmIdsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.Farms.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListFarmIdsByRegionIdsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> regionIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Farms.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.OperationalRegionId != null && regionIds.Contains(item.OperationalRegionId.Value))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> FarmExistsAsync(Guid organizationId, Guid farmId, CancellationToken cancellationToken = default) =>
        dbContext.Farms.AsNoTracking().AnyAsync(item => item.OrganizationId == organizationId && item.Id == farmId, cancellationToken);

    public void AddAssignment(FarmAccessAssignment assignment) => dbContext.FarmAccessAssignments.Add(assignment);
}
