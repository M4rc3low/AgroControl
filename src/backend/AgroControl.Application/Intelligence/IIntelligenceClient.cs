namespace AgroControl.Application.Intelligence;

public interface IIntelligenceClient
{
    Task<IntelligenceCallResult> PredictYieldAsync(
        SeasonPredictionData data,
        CancellationToken cancellationToken = default);

    Task<RasterIntelligenceCallResult> ProcessRasterAsync(
        RasterProcessingData data,
        CancellationToken cancellationToken = default);
}
