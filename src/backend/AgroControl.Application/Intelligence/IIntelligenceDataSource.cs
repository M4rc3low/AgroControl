namespace AgroControl.Application.Intelligence;

public interface IIntelligenceDataSource
{
    Task<SeasonPredictionData?> GetSeasonPredictionDataAsync(
        Guid organizationId,
        Guid seasonId,
        CancellationToken cancellationToken = default);
}
