package com.agrocontrol.telemetry.api;

import com.agrocontrol.telemetry.domain.DeviceStatus;
import com.agrocontrol.telemetry.domain.DeviceType;
import java.time.OffsetDateTime;
import java.util.Map;
import java.util.UUID;

public final class TelemetryContracts {
    private TelemetryContracts() {}

    public record CreateDeviceRequest(
        String externalKey,
        DeviceType deviceType,
        UUID machineId,
        UUID farmId,
        UUID fieldId,
        String credentialFingerprint
    ) {}

    public record UpdateDeviceStatusRequest(DeviceStatus status) {}

    public record TelemetryEventRequest(
        String eventId,
        OffsetDateTime capturedAtUtc,
        String metric,
        Double numericValue,
        String textValue,
        String unit,
        Double latitude,
        Double longitude,
        String quality,
        String source,
        Map<String, String> metadata
    ) {}

    public record IngestResponse(boolean duplicate, UUID eventId, OffsetDateTime receivedAtUtc) {}
}
