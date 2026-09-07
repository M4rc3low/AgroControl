package com.agrocontrol.telemetry.service;

import com.agrocontrol.telemetry.api.TelemetryContracts.TelemetryEventRequest;
import com.agrocontrol.telemetry.domain.DeviceStatus;
import com.agrocontrol.telemetry.domain.TelemetryEventRecord;
import com.agrocontrol.telemetry.repository.DeviceRepository;
import com.agrocontrol.telemetry.repository.TelemetryEventRepository;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Map;
import java.util.UUID;
import java.util.regex.Pattern;
import org.springframework.stereotype.Service;
import tools.jackson.databind.json.JsonMapper;

@Service
public class TelemetryIngestionService {
    private static final Pattern METRIC = Pattern.compile("[a-zA-Z0-9_.-]{1,80}");
    private final DeviceRepository devices;
    private final TelemetryEventRepository events;
    private final JsonMapper jsonMapper;

    public TelemetryIngestionService(DeviceRepository devices, TelemetryEventRepository events, JsonMapper jsonMapper) {
        this.devices = devices;
        this.events = events;
        this.jsonMapper = jsonMapper;
    }

    public IngestResult ingest(UUID organizationId, UUID deviceId, TelemetryEventRequest request, String transport) {
        var device = devices.find(organizationId, deviceId).orElseThrow(() -> new DeviceService.ResourceNotFoundException("Device not found."));
        if (device.status() != DeviceStatus.ACTIVE) throw new IllegalStateException("Device is inactive.");
        validate(request);

        var now = OffsetDateTime.now(ZoneOffset.UTC);
        var eventKey = normalize(request.eventId());
        var source = normalize(request.source());
        if (source == null) source = transport;
        var event = new TelemetryEventRecord(UUID.randomUUID(), organizationId, deviceId, eventKey, request.capturedAtUtc(), now,
            request.metric().trim().toLowerCase(), request.numericValue(), normalize(request.textValue()), request.unit().trim(),
            request.latitude(), request.longitude(), normalize(request.quality()), source, serializeMetadata(request.metadata()));

        if (events.insert(event)) return new IngestResult(event, false);
        var existing = events.findByEventKey(deviceId, eventKey).orElseThrow();
        return new IngestResult(existing, true);
    }

    public IngestResult ingestFromDevice(UUID deviceId, TelemetryEventRequest request, String transport) {
        var device = devices.findById(deviceId).orElseThrow(() -> new DeviceService.ResourceNotFoundException("Device not found."));
        return ingest(device.organizationId(), deviceId, request, transport);
    }

    private void validate(TelemetryEventRequest request) {
        if (request == null) throw new IllegalArgumentException("Telemetry payload is required.");
        if (request.capturedAtUtc() == null) throw new IllegalArgumentException("capturedAtUtc is required.");
        var now = OffsetDateTime.now(ZoneOffset.UTC);
        if (request.capturedAtUtc().isAfter(now.plusMinutes(5))) throw new IllegalArgumentException("capturedAtUtc is too far in the future.");
        if (request.capturedAtUtc().isBefore(now.minusDays(366))) throw new IllegalArgumentException("capturedAtUtc is older than the accepted ingestion window.");
        if (request.metric() == null || !METRIC.matcher(request.metric()).matches()) throw new IllegalArgumentException("metric is invalid.");
        if (request.unit() == null || request.unit().isBlank() || request.unit().length() > 40) throw new IllegalArgumentException("unit is required and must contain at most 40 characters.");
        var hasNumeric = request.numericValue() != null;
        var hasText = request.textValue() != null && !request.textValue().isBlank();
        if (hasNumeric == hasText) throw new IllegalArgumentException("Exactly one of numericValue or textValue must be supplied.");
        if (hasNumeric && !Double.isFinite(request.numericValue())) throw new IllegalArgumentException("numericValue must be finite.");
        if (hasText && request.textValue().length() > 500) throw new IllegalArgumentException("textValue must contain at most 500 characters.");
        if ((request.latitude() == null) != (request.longitude() == null)) throw new IllegalArgumentException("latitude and longitude must be supplied together.");
        if (request.latitude() != null && (request.latitude() < -90 || request.latitude() > 90)) throw new IllegalArgumentException("latitude is invalid.");
        if (request.longitude() != null && (request.longitude() < -180 || request.longitude() > 180)) throw new IllegalArgumentException("longitude is invalid.");
        if (request.eventId() != null && request.eventId().length() > 120) throw new IllegalArgumentException("eventId must contain at most 120 characters.");
        if (request.metadata() != null) {
            if (request.metadata().size() > 16) throw new IllegalArgumentException("metadata accepts at most 16 entries.");
            request.metadata().forEach((key, value) -> {
                if (key == null || key.isBlank() || key.length() > 60 || value == null || value.length() > 200)
                    throw new IllegalArgumentException("metadata contains an invalid key or value.");
            });
        }
    }

    private String serializeMetadata(Map<String, String> metadata) {
        if (metadata == null || metadata.isEmpty()) return null;
        try { return jsonMapper.writeValueAsString(metadata); }
        catch (Exception ex) { throw new IllegalArgumentException("metadata cannot be serialized.", ex); }
    }

    private static String normalize(String value) { return value == null || value.isBlank() ? null : value.trim(); }

    public record IngestResult(TelemetryEventRecord event, boolean duplicate) {}
}
