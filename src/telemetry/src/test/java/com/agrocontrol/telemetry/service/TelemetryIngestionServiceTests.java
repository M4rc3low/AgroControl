package com.agrocontrol.telemetry.service;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.*;

import com.agrocontrol.telemetry.api.TelemetryContracts.TelemetryEventRequest;
import com.agrocontrol.telemetry.domain.*;
import com.agrocontrol.telemetry.repository.DeviceRepository;
import com.agrocontrol.telemetry.repository.TelemetryEventRepository;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import tools.jackson.databind.json.JsonMapper;

class TelemetryIngestionServiceTests {
    @Test
    void mqtt_ingestion_uses_registered_device_tenant_not_payload_tenant() {
        var devices = mock(DeviceRepository.class);
        var events = mock(TelemetryEventRepository.class);
        var organizationId = UUID.randomUUID();
        var deviceId = UUID.randomUUID();
        var now = OffsetDateTime.now(ZoneOffset.UTC);
        var device = new DeviceRecord(deviceId, organizationId, "soil-01", DeviceType.FIELD_SENSOR, null, null, null, DeviceStatus.ACTIVE, null, now, now);
        when(devices.findById(deviceId)).thenReturn(Optional.of(device));
        when(devices.find(organizationId, deviceId)).thenReturn(Optional.of(device));
        when(events.insert(any())).thenReturn(true);
        var service = new TelemetryIngestionService(devices, events, JsonMapper.builder().build());
        var request = new TelemetryEventRequest("evt-1", now, "soil.moisture", 31.5, null, "%", null, null, "good", null, Map.of("depth", "20cm"));

        var result = service.ingestFromDevice(deviceId, request, "mqtt");

        assertEquals(organizationId, result.event().organizationId());
        assertEquals(deviceId, result.event().deviceId());
        assertEquals("mqtt", result.event().source());
    }

    @Test
    void rejects_payload_with_two_values() {
        var service = new TelemetryIngestionService(mock(DeviceRepository.class), mock(TelemetryEventRepository.class), JsonMapper.builder().build());
        var request = new TelemetryEventRequest(null, OffsetDateTime.now(ZoneOffset.UTC), "air.temperature", 23.0, "warm", "C", null, null, null, null, null);
        assertThrows(Exception.class, () -> service.ingest(UUID.randomUUID(), UUID.randomUUID(), request, "rest"));
    }
}
