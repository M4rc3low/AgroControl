package com.agrocontrol.telemetry.api;

import com.agrocontrol.telemetry.api.TelemetryContracts.CreateDeviceRequest;
import com.agrocontrol.telemetry.api.TelemetryContracts.IngestResponse;
import com.agrocontrol.telemetry.api.TelemetryContracts.TelemetryEventRequest;
import com.agrocontrol.telemetry.api.TelemetryContracts.UpdateDeviceStatusRequest;
import com.agrocontrol.telemetry.repository.TelemetryEventRepository;
import com.agrocontrol.telemetry.service.DeviceService;
import com.agrocontrol.telemetry.service.TelemetryIngestionService;
import java.time.OffsetDateTime;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

@RestController
public class TelemetryController {
    private final DeviceService devices;
    private final TelemetryIngestionService ingestion;
    private final TelemetryEventRepository events;

    public TelemetryController(DeviceService devices, TelemetryIngestionService ingestion, TelemetryEventRepository events) {
        this.devices = devices;
        this.ingestion = ingestion;
        this.events = events;
    }

    @GetMapping("/health")
    public Object health() { return java.util.Map.of("service", "AgroControl.Telemetry", "status", "running", "version", "0.8.0"); }

    @PostMapping("/api/v1/organizations/{organizationId}/devices")
    @ResponseStatus(HttpStatus.CREATED)
    public Object create(@PathVariable UUID organizationId, @RequestBody CreateDeviceRequest request) { return devices.create(organizationId, request); }

    @GetMapping("/api/v1/organizations/{organizationId}/devices")
    public Object list(@PathVariable UUID organizationId, @RequestParam(defaultValue = "100") int limit) { return devices.list(organizationId, limit); }

    @GetMapping("/api/v1/organizations/{organizationId}/devices/{deviceId}")
    public Object get(@PathVariable UUID organizationId, @PathVariable UUID deviceId) { return devices.get(organizationId, deviceId); }

    @PatchMapping("/api/v1/organizations/{organizationId}/devices/{deviceId}/status")
    public Object status(@PathVariable UUID organizationId, @PathVariable UUID deviceId, @RequestBody UpdateDeviceStatusRequest request) {
        return devices.updateStatus(organizationId, deviceId, request.status());
    }

    @PostMapping("/api/v1/organizations/{organizationId}/devices/{deviceId}/events")
    @ResponseStatus(HttpStatus.CREATED)
    public IngestResponse ingest(@PathVariable UUID organizationId, @PathVariable UUID deviceId, @RequestBody TelemetryEventRequest request) {
        var result = ingestion.ingest(organizationId, deviceId, request, "rest");
        return new IngestResponse(result.duplicate(), result.event().id(), result.event().receivedAtUtc());
    }

    @GetMapping("/api/v1/organizations/{organizationId}/devices/{deviceId}/latest")
    public Object latest(@PathVariable UUID organizationId, @PathVariable UUID deviceId, @RequestParam(required = false) String metric) {
        devices.get(organizationId, deviceId);
        return events.latest(organizationId, deviceId, metric).orElseThrow(() -> new DeviceService.ResourceNotFoundException("Telemetry event not found."));
    }

    @GetMapping("/api/v1/organizations/{organizationId}/devices/{deviceId}/events")
    public Object history(@PathVariable UUID organizationId, @PathVariable UUID deviceId,
                          @RequestParam(required = false) String metric,
                          @RequestParam(required = false) OffsetDateTime from,
                          @RequestParam(required = false) OffsetDateTime to,
                          @RequestParam(defaultValue = "100") int limit) {
        devices.get(organizationId, deviceId);
        if (from != null && to != null && from.isAfter(to)) throw new IllegalArgumentException("from cannot be after to.");
        return events.history(organizationId, deviceId, metric, from, to, Math.clamp(limit, 1, 500));
    }
}
