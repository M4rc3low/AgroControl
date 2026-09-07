package com.agrocontrol.telemetry;

import static org.junit.jupiter.api.Assertions.*;

import com.agrocontrol.telemetry.api.TelemetryContracts.CreateDeviceRequest;
import com.agrocontrol.telemetry.api.TelemetryContracts.TelemetryEventRequest;
import com.agrocontrol.telemetry.domain.DeviceType;
import com.agrocontrol.telemetry.repository.TelemetryEventRepository;
import com.agrocontrol.telemetry.service.DeviceService;
import com.agrocontrol.telemetry.service.TelemetryIngestionService;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.jdbc.core.JdbcTemplate;

@SpringBootTest(properties = "telemetry.mqtt.enabled=false")
@EnabledIfEnvironmentVariable(named = "AGROCONTROL_TELEMETRY_TESTS", matches = "true")
class TelemetryPersistenceIntegrationTests {
    @Autowired DeviceService devices;
    @Autowired TelemetryIngestionService ingestion;
    @Autowired TelemetryEventRepository events;
    @Autowired JdbcTemplate jdbc;

    @BeforeEach
    void clean() {
        jdbc.update("DELETE FROM telemetry_events");
        jdbc.update("DELETE FROM telemetry_devices");
    }

    @Test
    void persists_append_only_event_and_deduplicates_event_key() {
        var organizationId = UUID.randomUUID();
        var device = devices.create(organizationId, new CreateDeviceRequest("weather-ci", DeviceType.WEATHER_STATION, null, null, null, null));
        var capturedAt = OffsetDateTime.now(ZoneOffset.UTC).minusSeconds(2);
        var request = new TelemetryEventRequest("evt-ci-1", capturedAt, "air.temperature", 28.4, null, "C", -23.55, -46.63, "good", "integration-test", Map.of("station", "roof"));

        var first = ingestion.ingest(organizationId, device.id(), request, "rest");
        var duplicate = ingestion.ingest(organizationId, device.id(), request, "rest");

        assertFalse(first.duplicate());
        assertTrue(duplicate.duplicate());
        assertEquals(first.event().id(), duplicate.event().id());
        assertEquals(1, events.history(organizationId, device.id(), null, null, null, 100).size());
    }

    @Test
    void query_cannot_cross_organization_boundary() {
        var organizationA = UUID.randomUUID();
        var organizationB = UUID.randomUUID();
        var device = devices.create(organizationA, new CreateDeviceRequest("soil-a", DeviceType.FIELD_SENSOR, null, null, null, null));
        var request = new TelemetryEventRequest(null, OffsetDateTime.now(ZoneOffset.UTC), "soil.moisture", 44.0, null, "%", null, null, null, null, null);
        ingestion.ingest(organizationA, device.id(), request, "rest");

        assertTrue(events.latest(organizationB, device.id(), null).isEmpty());
        assertThrows(DeviceService.ResourceNotFoundException.class, () -> devices.get(organizationB, device.id()));
    }
}
