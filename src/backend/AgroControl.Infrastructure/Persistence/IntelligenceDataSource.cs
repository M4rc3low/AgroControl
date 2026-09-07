using AgroControl.Application.Intelligence;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class IntelligenceDataSource(AgroControlDbContext dbContext) : IIntelligenceDataSource
{
    public async Task<SeasonPredictionData?> GetSeasonPredictionDataAsync(
        Guid organizationId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        var current = await (
            from season in dbContext.Seasons.AsNoTracking()
            join field in dbContext.Fields.AsNoTracking() on season.FieldId equals field.Id
            join crop in dbContext.Crops.AsNoTracking() on season.CropId equals crop.Id
            where season.OrganizationId == organizationId
                && field.OrganizationId == organizationId
                && crop.OrganizationId == organizationId
                && season.Id == seasonId
                && season.IsActive
                && field.IsActive
                && crop.IsActive
            select new
            {
                season.Id,
                season.CropId,
                CropName = crop.Name,
                CropVariety = crop.Variety,
                field.AreaHectares,
                season.ExpectedYieldPerHectare
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null)
            return null;

        var historyRows = await (
            from season in dbContext.Seasons.AsNoTracking()
            join field in dbContext.Fields.AsNoTracking() on season.FieldId equals field.Id
            where season.OrganizationId == organizationId
                && field.OrganizationId == organizationId
                && season.CropId == current.CropId
                && season.Id != current.Id
                && season.IsActive
                && field.IsActive
                && season.ActualYieldPerHectare != null
            orderby season.StartDate descending
            select new
            {
                field.AreaHectares,
                season.ExpectedYieldPerHectare,
                ActualYieldPerHectare = season.ActualYieldPerHectare!.Value
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        var historicalSamples = historyRows
            .Select(row => new HistoricalYieldSample(
                row.AreaHectares,
                row.ExpectedYieldPerHectare,
                row.ActualYieldPerHectare))
            .ToList();

        return new SeasonPredictionData(
            current.Id,
            current.CropName,
            current.CropVariety,
            current.AreaHectares,
            current.ExpectedYieldPerHectare,
            historicalSamples);
    }
}
