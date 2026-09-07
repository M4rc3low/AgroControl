namespace AgroControl.Application.Telemetry;

public interface ITelemetryClient
{
    Task<IReadOnlyList<TelemetryDeviceDto>> ListDevicesAsync(Guid organizationId, int limit, CancellationToken cancellationToken = default);
    Task<TelemetryDeviceDto> CreateDeviceAsync(Guid organizationId, CreateTelemetryDeviceCommand command, CancellationToken cancellationToken = default);
    Task<TelemetryDeviceDto> GetDeviceAsync(Guid organizationId, Guid deviceId, CancellationToken cancellationToken = default);
    Task<TelemetryDeviceDto> UpdateStatusAsync(Guid organizationId, Guid deviceId, string status, CancellationToken cancellationToken = default);
    Task<TelemetryIngestResponse> IngestAsync(Guid organizationId, Guid deviceId, CreateTelemetryEventCommand command, CancellationToken cancellationToken = default);
    Task<TelemetryEventDto> GetLatestAsync(Guid organizationId, Guid deviceId, string? metric, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TelemetryEventDto>> GetHistoryAsync(Guid organizationId, Guid deviceId, string? metric, DateTimeOffset? from, DateTimeOffset? to, int limit, CancellationToken cancellationToken = default);
}
