package com.agrocontrol.telemetry.repository;

import com.agrocontrol.telemetry.domain.DeviceRecord;
import com.agrocontrol.telemetry.domain.DeviceStatus;
import com.agrocontrol.telemetry.domain.DeviceType;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

@Repository
public class DeviceRepository {
    private final JdbcTemplate jdbc;

    public DeviceRepository(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    public DeviceRecord insert(DeviceRecord device) {
        jdbc.update("""
            INSERT INTO telemetry_devices
              (id, organization_id, external_key, device_type, machine_id, farm_id, field_id, status, credential_fingerprint, created_at_utc, updated_at_utc)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            device.id(), device.organizationId(), device.externalKey(), device.deviceType().name(), device.machineId(), device.farmId(),
            device.fieldId(), device.status().name(), device.credentialFingerprint(), device.createdAtUtc(), device.updatedAtUtc());
        return device;
    }

    public Optional<DeviceRecord> find(UUID organizationId, UUID deviceId) {
        return jdbc.query("SELECT * FROM telemetry_devices WHERE organization_id = ? AND id = ?", this::map, organizationId, deviceId)
            .stream().findFirst();
    }

    public Optional<DeviceRecord> findById(UUID deviceId) {
        return jdbc.query("SELECT * FROM telemetry_devices WHERE id = ?", this::map, deviceId).stream().findFirst();
    }

    public List<DeviceRecord> list(UUID organizationId, int limit) {
        return jdbc.query("SELECT * FROM telemetry_devices WHERE organization_id = ? ORDER BY created_at_utc DESC LIMIT ?", this::map, organizationId, limit);
    }

    public DeviceRecord updateStatus(UUID organizationId, UUID deviceId, DeviceStatus status, OffsetDateTime now) {
        var updated = jdbc.update("UPDATE telemetry_devices SET status = ?, updated_at_utc = ? WHERE organization_id = ? AND id = ?", status.name(), now, organizationId, deviceId);
        if (updated == 0) throw new IllegalArgumentException("Device not found.");
        return find(organizationId, deviceId).orElseThrow();
    }

    private DeviceRecord map(ResultSet rs, int rowNum) throws SQLException {
        return new DeviceRecord(
            rs.getObject("id", UUID.class),
            rs.getObject("organization_id", UUID.class),
            rs.getString("external_key"),
            DeviceType.valueOf(rs.getString("device_type")),
            rs.getObject("machine_id", UUID.class),
            rs.getObject("farm_id", UUID.class),
            rs.getObject("field_id", UUID.class),
            DeviceStatus.valueOf(rs.getString("status")),
            rs.getString("credential_fingerprint"),
            rs.getObject("created_at_utc", OffsetDateTime.class),
            rs.getObject("updated_at_utc", OffsetDateTime.class));
    }
}
