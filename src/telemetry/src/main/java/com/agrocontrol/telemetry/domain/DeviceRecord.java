package com.agrocontrol.telemetry.domain;

import java.time.OffsetDateTime;
import java.util.UUID;

public record DeviceRecord(
    UUID id,
    UUID organizationId,
    String externalKey,
    DeviceType deviceType,
    UUID machineId,
    UUID farmId,
    UUID fieldId,
    DeviceStatus status,
    String credentialFingerprint,
    OffsetDateTime createdAtUtc,
    OffsetDateTime updatedAtUtc
) {}
