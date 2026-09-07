package com.agrocontrol.telemetry.service;

import com.agrocontrol.telemetry.api.TelemetryContracts.CreateDeviceRequest;
import com.agrocontrol.telemetry.domain.DeviceRecord;
import com.agrocontrol.telemetry.domain.DeviceStatus;
import com.agrocontrol.telemetry.repository.DeviceRepository;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;
import org.springframework.dao.DuplicateKeyException;
import org.springframework.stereotype.Service;

@Service
public class DeviceService {
    private final DeviceRepository repository;

    public DeviceService(DeviceRepository repository) {
        this.repository = repository;
    }

    public DeviceRecord create(UUID organizationId, CreateDeviceRequest request) {
        if (organizationId == null) throw new IllegalArgumentException("Organization id is required.");
        if (request.externalKey() == null || request.externalKey().isBlank() || request.externalKey().length() > 120)
            throw new IllegalArgumentException("externalKey is required and must contain at most 120 characters.");
        if (request.deviceType() == null) throw new IllegalArgumentException("deviceType is required.");
        if (request.credentialFingerprint() != null && request.credentialFingerprint().length() > 160)
            throw new IllegalArgumentException("credentialFingerprint must contain at most 160 characters.");

        var now = OffsetDateTime.now(ZoneOffset.UTC);
        var device = new DeviceRecord(UUID.randomUUID(), organizationId, request.externalKey().trim(), request.deviceType(), request.machineId(),
            request.farmId(), request.fieldId(), DeviceStatus.ACTIVE, normalize(request.credentialFingerprint()), now, now);
        try {
            return repository.insert(device);
        } catch (DuplicateKeyException ex) {
            throw new IllegalStateException("A device with this externalKey already exists in the organization.");
        }
    }

    public DeviceRecord get(UUID organizationId, UUID deviceId) {
        return repository.find(organizationId, deviceId).orElseThrow(() -> new ResourceNotFoundException("Device not found."));
    }

    public List<DeviceRecord> list(UUID organizationId, int limit) {
        return repository.list(organizationId, Math.clamp(limit, 1, 200));
    }

    public DeviceRecord updateStatus(UUID organizationId, UUID deviceId, DeviceStatus status) {
        if (status == null) throw new IllegalArgumentException("status is required.");
        get(organizationId, deviceId);
        return repository.updateStatus(organizationId, deviceId, status, OffsetDateTime.now(ZoneOffset.UTC));
    }

    private static String normalize(String value) {
        return value == null || value.isBlank() ? null : value.trim();
    }

    public static final class ResourceNotFoundException extends RuntimeException {
        public ResourceNotFoundException(String message) { super(message); }
    }
}
