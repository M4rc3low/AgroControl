namespace AgroControl.Application.Telemetry;

public sealed record CreateTelemetryDeviceCommand(
    string ExternalKey,
    string DeviceType,
    Guid? MachineId,
    Guid? FarmId,
    Guid? FieldId,
    string? CredentialFingerprint);

public sealed record TelemetryDeviceDto(
    Guid Id,
    Guid OrganizationId,
    string ExternalKey,
    string DeviceType,
    Guid? MachineId,
    Guid? FarmId,
    Guid? FieldId,
    string Status,
    string? CredentialFingerprint,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateTelemetryEventCommand(
    string? EventId,
    DateTimeOffset CapturedAtUtc,
    string Metric,
    double? NumericValue,
    string? TextValue,
    string Unit,
    double? Latitude,
    double? Longitude,
    string? Quality,
    string? Source,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record TelemetryEventDto(
    Guid Id,
    Guid OrganizationId,
    Guid DeviceId,
    string? EventKey,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset ReceivedAtUtc,
    string Metric,
    double? NumericValue,
    string? TextValue,
    string Unit,
    double? Latitude,
    double? Longitude,
    string? Quality,
    string Source,
    string? MetadataJson);

public sealed record TelemetryIngestResponse(bool Duplicate, Guid EventId, DateTimeOffset ReceivedAtUtc);

public enum TelemetryClientErrorKind { NotFound, Validation, Conflict, Timeout, Unavailable }

public sealed class TelemetryClientException(TelemetryClientErrorKind kind, string message, Exception? inner = null) : Exception(message, inner)
{
    public TelemetryClientErrorKind Kind { get; } = kind;
}
