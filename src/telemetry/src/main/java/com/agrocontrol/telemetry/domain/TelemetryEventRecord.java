package com.agrocontrol.telemetry.domain;

import java.time.OffsetDateTime;
import java.util.UUID;

public record TelemetryEventRecord(
    UUID id,
    UUID organizationId,
    UUID deviceId,
    String eventKey,
    OffsetDateTime capturedAtUtc,
    OffsetDateTime receivedAtUtc,
    String metric,
    Double numericValue,
    String textValue,
    String unit,
    Double latitude,
    Double longitude,
    String quality,
    String source,
    String metadataJson
) {}
