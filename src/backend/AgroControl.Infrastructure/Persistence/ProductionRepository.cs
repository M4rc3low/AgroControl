using AgroControl.Application.Production;
using AgroControl.Domain.Modules.Crops;
using AgroControl.Domain.Modules.Farms;
using AgroControl.Domain.Modules.Fields;
using AgroControl.Domain.Modules.Seasons;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class ProductionRepository(AgroControlDbContext dbContext) : IProductionRepository
{
    public async Task<(IReadOnlyList<Farm> Items, int TotalCount)> ListFarmsAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Farms.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Farm?> GetFarmAsync(Guid organizationId, Guid farmId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Farm> query = dbContext.Farms.Where(item => item.OrganizationId == organizationId && item.Id == farmId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public void AddFarm(Farm farm) => dbContext.Farms.Add(farm);

    public async Task<(IReadOnlyList<Field> Items, int TotalCount)> ListFieldsAsync(Guid organizationId, int skip, int take, Guid? farmId, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Fields.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (farmId is not null) query = query.Where(item => item.FarmId == farmId);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Field?> GetFieldAsync(Guid organizationId, Guid fieldId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Field> query = dbContext.Fields.Where(item => item.OrganizationId == organizationId && item.Id == fieldId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetAllocatedFieldAreaAsync(Guid organizationId, Guid farmId, Guid? excludingFieldId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Fields
            .AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.FarmId == farmId && item.IsActive);

        if (excludingFieldId is not null)
            query = query.Where(item => item.Id != excludingFieldId.Value);

        return await query.SumAsync(item => (decimal?)item.AreaHectares, cancellationToken) ?? 0m;
    }

    public void AddField(Field field) => dbContext.Fields.Add(field);

    public async Task<(IReadOnlyList<Crop> Items, int TotalCount)> ListCropsAsync(Guid organizationId, int skip, int take, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Crops.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(term) || (item.Variety != null && item.Variety.ToLower().Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Name).ThenBy(item => item.Variety).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Crop?> GetCropAsync(Guid organizationId, Guid cropId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Crop> query = dbContext.Crops.Where(item => item.OrganizationId == organizationId && item.Id == cropId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public void AddCrop(Crop crop) => dbContext.Crops.Add(crop);

    public async Task<(IReadOnlyList<Season> Items, int TotalCount)> ListSeasonsAsync(Guid organizationId, int skip, int take, Guid? fieldId, SeasonStatus? status, string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Seasons.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (fieldId is not null) query = query.Where(item => item.FieldId == fieldId);
        if (status is not null) query = query.Where(item => item.Status == status);
        if (!includeInactive) query = query.Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.StartDate).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Season?> GetSeasonAsync(Guid organizationId, Guid seasonId, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Season> query = dbContext.Seasons.Where(item => item.OrganizationId == organizationId && item.Id == seasonId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public void AddSeason(Season season) => dbContext.Seasons.Add(season);
}
