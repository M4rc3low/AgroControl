using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgroControl.Application.Telemetry;
using Microsoft.Extensions.Configuration;

namespace AgroControl.Infrastructure.Telemetry;

public sealed class TelemetryHttpClient(HttpClient httpClient, IConfiguration configuration) : ITelemetryClient
{
    private readonly string _internalKey = configuration["Telemetry:InternalApiKey"]
        ?? throw new InvalidOperationException("Telemetry:InternalApiKey is required.");

    public Task<IReadOnlyList<TelemetryDeviceDto>> ListDevicesAsync(Guid organizationId, int limit, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<TelemetryDeviceDto>>(HttpMethod.Get, $"/api/v1/organizations/{organizationId}/devices?limit={Math.Clamp(limit, 1, 200)}", null, cancellationToken);

    public Task<TelemetryDeviceDto> CreateDeviceAsync(Guid organizationId, CreateTelemetryDeviceCommand command, CancellationToken cancellationToken = default) =>
        SendAsync<TelemetryDeviceDto>(HttpMethod.Post, $"/api/v1/organizations/{organizationId}/devices", command, cancellationToken);

    public Task<TelemetryDeviceDto> GetDeviceAsync(Guid organizationId, Guid deviceId, CancellationToken cancellationToken = default) =>
        SendAsync<TelemetryDeviceDto>(HttpMethod.Get, $"/api/v1/organizations/{organizationId}/devices/{deviceId}", null, cancellationToken);

    public Task<TelemetryDeviceDto> UpdateStatusAsync(Guid organizationId, Guid deviceId, string status, CancellationToken cancellationToken = default) =>
        SendAsync<TelemetryDeviceDto>(HttpMethod.Patch, $"/api/v1/organizations/{organizationId}/devices/{deviceId}/status", new { status }, cancellationToken);

    public Task<TelemetryIngestResponse> IngestAsync(Guid organizationId, Guid deviceId, CreateTelemetryEventCommand command, CancellationToken cancellationToken = default) =>
        SendAsync<TelemetryIngestResponse>(HttpMethod.Post, $"/api/v1/organizations/{organizationId}/devices/{deviceId}/events", command, cancellationToken);

    public Task<TelemetryEventDto> GetLatestAsync(Guid organizationId, Guid deviceId, string? metric, CancellationToken cancellationToken = default) =>
        SendAsync<TelemetryEventDto>(HttpMethod.Get, $"/api/v1/organizations/{organizationId}/devices/{deviceId}/latest{QueryMetric(metric)}", null, cancellationToken);

    public Task<IReadOnlyList<TelemetryEventDto>> GetHistoryAsync(Guid organizationId, Guid deviceId, string? metric, DateTimeOffset? from, DateTimeOffset? to, int limit, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"limit={Math.Clamp(limit, 1, 500)}" };
        if (!string.IsNullOrWhiteSpace(metric)) query.Add($"metric={Uri.EscapeDataString(metric.Trim())}");
        if (from is not null) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to is not null) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        return SendAsync<IReadOnlyList<TelemetryEventDto>>(HttpMethod.Get, $"/api/v1/organizations/{organizationId}/devices/{deviceId}/events?{string.Join('&', query)}", null, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("X-AgroControl-Internal-Key", _internalKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new TelemetryClientException(Map(response.StatusCode), string.IsNullOrWhiteSpace(detail) ? $"Telemetry returned {(int)response.StatusCode}." : detail);
            }
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return value ?? throw new TelemetryClientException(TelemetryClientErrorKind.Unavailable, "Telemetry returned an empty response.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TelemetryClientException(TelemetryClientErrorKind.Timeout, "Telemetry request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new TelemetryClientException(TelemetryClientErrorKind.Unavailable, "Telemetry service is unavailable.", ex);
        }
        catch (JsonException ex)
        {
            throw new TelemetryClientException(TelemetryClientErrorKind.Unavailable, "Telemetry returned an invalid response.", ex);
        }
    }

    private static TelemetryClientErrorKind Map(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => TelemetryClientErrorKind.NotFound,
        HttpStatusCode.UnprocessableEntity or HttpStatusCode.BadRequest => TelemetryClientErrorKind.Validation,
        HttpStatusCode.Conflict => TelemetryClientErrorKind.Conflict,
        HttpStatusCode.GatewayTimeout => TelemetryClientErrorKind.Timeout,
        _ => TelemetryClientErrorKind.Unavailable
    };

    private static string QueryMetric(string? metric) => string.IsNullOrWhiteSpace(metric) ? string.Empty : $"?metric={Uri.EscapeDataString(metric.Trim())}";
}
