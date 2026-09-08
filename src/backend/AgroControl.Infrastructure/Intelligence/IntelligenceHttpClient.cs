using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
                var detail = await ReadErrorAsync(response, cancellationToken);
                return IntelligenceCallResult.Validation(detail ?? "Intelligence service rejected the prediction payload.");
            }

            if (!response.IsSuccessStatusCode)
                return IntelligenceCallResult.Unavailable($"Intelligence service returned HTTP {(int)response.StatusCode}.");

            var payload = await response.Content.ReadFromJsonAsync<PredictionResponse>(cancellationToken: cancellationToken);
            if (payload is null) return IntelligenceCallResult.Unavailable("Intelligence service returned an empty response.");

            var metrics = payload.Metrics is null ? null : new YieldPredictionMetrics(payload.Metrics.Mae, payload.Metrics.Rmse);
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
            return IntelligenceCallResult.Timeout("Intelligence service did not respond within the configured timeout.");
        }
        catch (HttpRequestException ex)
        {
            return IntelligenceCallResult.Unavailable($"Intelligence service could not be reached: {ex.Message}");
        }
    }

    public async Task<RasterIntelligenceCallResult> ProcessRasterAsync(
        RasterProcessingData data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var targets = new List<RasterTargetRequest>(data.Targets.Count);
            foreach (var target in data.Targets)
            {
                using var document = JsonDocument.Parse(target.GeometryGeoJson);
                targets.Add(new RasterTargetRequest(target.Key, document.RootElement.Clone()));
            }

            var request = new RasterRequest("v1", data.AssetReference, data.GeometryCrs, data.Band, targets);
            using var response = await httpClient.PostAsJsonAsync("/api/v1/raster/zonal-statistics", request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var detail = await ReadErrorAsync(response, cancellationToken);
                return RasterIntelligenceCallResult.Validation(detail ?? "Intelligence service rejected the raster payload.");
            }

            if (!response.IsSuccessStatusCode)
                return RasterIntelligenceCallResult.Unavailable($"Intelligence service returned HTTP {(int)response.StatusCode} during raster processing.");

            var payload = await response.Content.ReadFromJsonAsync<RasterResponse>(cancellationToken: cancellationToken);
            if (payload is null) return RasterIntelligenceCallResult.Unavailable("Intelligence service returned an empty raster response.");
            if (!string.Equals(payload.ContractVersion, "v1", StringComparison.Ordinal))
                return RasterIntelligenceCallResult.Unavailable("Intelligence raster contract version is not supported.");

            var result = new RasterProcessingIntelligenceDto(
                payload.ContractVersion,
                payload.Status,
                new RasterMetadataDto(payload.Metadata.Crs, payload.Metadata.Width, payload.Metadata.Height,
                    payload.Metadata.Nodata, payload.Metadata.ResolutionX, payload.Metadata.ResolutionY),
                payload.Results.Select(item => new RasterTargetStatisticsDto(item.Key, item.Statistics.Minimum,
                    item.Statistics.Maximum, item.Statistics.Mean, item.Statistics.Median,
                    item.Statistics.StandardDeviation, item.Statistics.ValidCoveragePercent,
                    item.Statistics.SampleCount)).ToList());
            return RasterIntelligenceCallResult.Success(result);
        }
        catch (JsonException ex)
        {
            return RasterIntelligenceCallResult.Validation($"Raster geometry could not be serialized: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RasterIntelligenceCallResult.Timeout("Intelligence raster processing exceeded the configured timeout.");
        }
        catch (HttpRequestException ex)
        {
            return RasterIntelligenceCallResult.Unavailable($"Intelligence service could not be reached: {ex.Message}");
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var document = JsonDocument.Parse(raw);
            return document.RootElement.TryGetProperty("detail", out var detail)
                ? detail.ValueKind == JsonValueKind.String ? detail.GetString() : detail.ToString()
                : raw;
        }
        catch (JsonException)
        {
            return raw;
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

    private sealed record RasterTargetRequest(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("geometry")] JsonElement Geometry);

    private sealed record RasterRequest(
        [property: JsonPropertyName("contract_version")] string ContractVersion,
        [property: JsonPropertyName("asset_reference")] string AssetReference,
        [property: JsonPropertyName("geometry_crs")] string GeometryCrs,
        [property: JsonPropertyName("band")] int Band,
        [property: JsonPropertyName("targets")] IReadOnlyList<RasterTargetRequest> Targets);

    private sealed record RasterMetadataResponse(
        [property: JsonPropertyName("crs")] string Crs,
        [property: JsonPropertyName("width")] int Width,
        [property: JsonPropertyName("height")] int Height,
        [property: JsonPropertyName("nodata")] decimal? Nodata,
        [property: JsonPropertyName("resolution_x")] decimal ResolutionX,
        [property: JsonPropertyName("resolution_y")] decimal ResolutionY);

    private sealed record RasterStatisticsResponse(
        [property: JsonPropertyName("minimum")] decimal Minimum,
        [property: JsonPropertyName("maximum")] decimal Maximum,
        [property: JsonPropertyName("mean")] decimal Mean,
        [property: JsonPropertyName("median")] decimal Median,
        [property: JsonPropertyName("standard_deviation")] decimal StandardDeviation,
        [property: JsonPropertyName("valid_coverage_percent")] decimal ValidCoveragePercent,
        [property: JsonPropertyName("sample_count")] long SampleCount);

    private sealed record RasterTargetResponse(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("statistics")] RasterStatisticsResponse Statistics);

    private sealed record RasterResponse(
        [property: JsonPropertyName("contract_version")] string ContractVersion,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("metadata")] RasterMetadataResponse Metadata,
        [property: JsonPropertyName("results")] IReadOnlyList<RasterTargetResponse> Results);
}
