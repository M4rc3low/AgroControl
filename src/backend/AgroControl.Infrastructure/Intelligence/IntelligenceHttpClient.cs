using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AgroControl.Application.Intelligence;

namespace AgroControl.Infrastructure.Intelligence;

public sealed class IntelligenceHttpClient(HttpClient httpClient) : IIntelligenceClient
{
    public async Task<IntelligenceCallResult> PredictYieldAsync(
        SeasonPredictionData data,
        CancellationToken cancellationToken = default)
    {
        var request = new PredictionRequest(
            "v1",
            data.CropName,
            data.CropVariety,
            data.AreaHectares,
            data.ExpectedYieldPerHectare,
            data.HistoricalSamples
                .Select(sample => new HistoricalSampleRequest(
                    sample.AreaHectares,
                    sample.ExpectedYieldPerHectare,
                    sample.ActualYieldPerHectare))
                .ToList());

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "/api/v1/yield/predict",
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                return IntelligenceCallResult.Validation(
                    string.IsNullOrWhiteSpace(detail)
                        ? "Intelligence service rejected the prediction payload."
                        : detail);
            }

            if (!response.IsSuccessStatusCode)
            {
                return IntelligenceCallResult.Unavailable(
                    $"Intelligence service returned HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<PredictionResponse>(
                cancellationToken: cancellationToken);

            if (payload is null)
                return IntelligenceCallResult.Unavailable(
                    "Intelligence service returned an empty response.");

            var metrics = payload.Metrics is null
                ? null
                : new YieldPredictionMetrics(payload.Metrics.Mae, payload.Metrics.Rmse);

            return IntelligenceCallResult.Success(new YieldPredictionDto(
                payload.ContractVersion,
                payload.Status,
                payload.PredictedYieldPerHectare,
                payload.BaselineYieldPerHectare,
                payload.ModelKind,
                payload.ModelVersion,
                payload.SampleCount,
                metrics,
                payload.GeneratedAtUtc,
                payload.Warning));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return IntelligenceCallResult.Timeout(
                "Intelligence service did not respond within the configured timeout.");
        }
        catch (HttpRequestException ex)
        {
            return IntelligenceCallResult.Unavailable(
                $"Intelligence service could not be reached: {ex.Message}");
        }
    }

    private sealed record HistoricalSampleRequest(
        [property: JsonPropertyName("area_hectares")] decimal AreaHectares,
        [property: JsonPropertyName("expected_yield_per_hectare")] decimal? ExpectedYieldPerHectare,
        [property: JsonPropertyName("actual_yield_per_hectare")] decimal ActualYieldPerHectare);

    private sealed record PredictionRequest(
        [property: JsonPropertyName("contract_version")] string ContractVersion,
        [property: JsonPropertyName("crop_name")] string CropName,
        [property: JsonPropertyName("crop_variety")] string? CropVariety,
        [property: JsonPropertyName("area_hectares")] decimal AreaHectares,
        [property: JsonPropertyName("expected_yield_per_hectare")] decimal? ExpectedYieldPerHectare,
        [property: JsonPropertyName("historical_samples")] IReadOnlyList<HistoricalSampleRequest> HistoricalSamples);

    private sealed record PredictionMetricsResponse(
        [property: JsonPropertyName("mae")] decimal Mae,
        [property: JsonPropertyName("rmse")] decimal Rmse);

    private sealed record PredictionResponse(
        [property: JsonPropertyName("contract_version")] string ContractVersion,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("predicted_yield_per_hectare")] decimal? PredictedYieldPerHectare,
        [property: JsonPropertyName("baseline_yield_per_hectare")] decimal? BaselineYieldPerHectare,
        [property: JsonPropertyName("model_kind")] string ModelKind,
        [property: JsonPropertyName("model_version")] string ModelVersion,
        [property: JsonPropertyName("sample_count")] int SampleCount,
        [property: JsonPropertyName("metrics")] PredictionMetricsResponse? Metrics,
        [property: JsonPropertyName("generated_at_utc")] DateTime GeneratedAtUtc,
        [property: JsonPropertyName("warning")] string? Warning);
}
