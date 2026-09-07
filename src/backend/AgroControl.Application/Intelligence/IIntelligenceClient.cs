namespace AgroControl.Application.Intelligence;

public interface IIntelligenceClient
{
    Task<IntelligenceCallResult> PredictYieldAsync(
        SeasonPredictionData data,
        CancellationToken cancellationToken = default);
}
