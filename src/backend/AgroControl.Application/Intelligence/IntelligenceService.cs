namespace AgroControl.Application.Intelligence;

public sealed class IntelligenceService(
    IIntelligenceDataSource dataSource,
    IIntelligenceClient client)
{
    public async Task<PredictionResult> PredictSeasonYieldAsync(
        Guid organizationId,
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        var data = await dataSource.GetSeasonPredictionDataAsync(
            organizationId,
            seasonId,
            cancellationToken);

        if (data is null)
            return PredictionResult.NotFound("Season not found for this organization.");

        var call = await client.PredictYieldAsync(data, cancellationToken);
        if (!call.Succeeded)
        {
            return call.ErrorKind switch
            {
                IntelligenceCallErrorKind.Validation =>
                    PredictionResult.Validation(call.Error ?? "Intelligence request was rejected."),
                IntelligenceCallErrorKind.Timeout =>
                    PredictionResult.Timeout(call.Error ?? "Intelligence service timed out."),
                _ => PredictionResult.Unavailable(
                    call.Error ?? "Intelligence service is unavailable.")
            };
        }

        var prediction = call.Value!;
        return string.Equals(
                prediction.Status,
                "insufficient_data",
                StringComparison.OrdinalIgnoreCase)
            ? PredictionResult.InsufficientData(prediction)
            : PredictionResult.Success(prediction);
    }
}
