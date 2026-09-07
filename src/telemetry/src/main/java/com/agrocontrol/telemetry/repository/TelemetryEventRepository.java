package com.agrocontrol.telemetry.repository;

import com.agrocontrol.telemetry.domain.TelemetryEventRecord;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

@Repository
public class TelemetryEventRepository {
    private final JdbcTemplate jdbc;

    public TelemetryEventRepository(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    public boolean insert(TelemetryEventRecord event) {
        var count = jdbc.update("""
            INSERT INTO telemetry_events
              (id, organization_id, device_id, event_key, captured_at_utc, received_at_utc, metric, numeric_value, text_value, unit, latitude, longitude, quality, source, metadata_json)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (device_id, event_key) WHERE event_key IS NOT NULL DO NOTHING
            """,
            event.id(), event.organizationId(), event.deviceId(), event.eventKey(), event.capturedAtUtc(), event.receivedAtUtc(),
            event.metric(), event.numericValue(), event.textValue(), event.unit(), event.latitude(), event.longitude(), event.quality(),
            event.source(), event.metadataJson());
        return count == 1;
    }

    public Optional<TelemetryEventRecord> findByEventKey(UUID deviceId, String eventKey) {
        if (eventKey == null) return Optional.empty();
        return jdbc.query("SELECT * FROM telemetry_events WHERE device_id = ? AND event_key = ?", this::map, deviceId, eventKey)
            .stream().findFirst();
    }

    public Optional<TelemetryEventRecord> latest(UUID organizationId, UUID deviceId, String metric) {
        var sql = metric == null || metric.isBlank()
            ? "SELECT * FROM telemetry_events WHERE organization_id = ? AND device_id = ? ORDER BY captured_at_utc DESC LIMIT 1"
            : "SELECT * FROM telemetry_events WHERE organization_id = ? AND device_id = ? AND metric = ? ORDER BY captured_at_utc DESC LIMIT 1";
        var rows = metric == null || metric.isBlank()
            ? jdbc.query(sql, this::map, organizationId, deviceId)
            : jdbc.query(sql, this::map, organizationId, deviceId, metric.trim().toLowerCase());
        return rows.stream().findFirst();
    }

    public List<TelemetryEventRecord> history(UUID organizationId, UUID deviceId, String metric, OffsetDateTime from, OffsetDateTime to, int limit) {
        var sql = new StringBuilder("SELECT * FROM telemetry_events WHERE organization_id = ? AND device_id = ?");
        var args = new java.util.ArrayList<Object>();
        args.add(organizationId);
        args.add(deviceId);
        if (metric != null && !metric.isBlank()) { sql.append(" AND metric = ?"); args.add(metric.trim().toLowerCase()); }
        if (from != null) { sql.append(" AND captured_at_utc >= ?"); args.add(from); }
        if (to != null) { sql.append(" AND captured_at_utc <= ?"); args.add(to); }
        sql.append(" ORDER BY captured_at_utc DESC LIMIT ?");
        args.add(limit);
        return jdbc.query(sql.toString(), this::map, args.toArray());
    }

    private TelemetryEventRecord map(ResultSet rs, int rowNum) throws SQLException {
        return new TelemetryEventRecord(
            rs.getObject("id", UUID.class), rs.getObject("organization_id", UUID.class), rs.getObject("device_id", UUID.class),
            rs.getString("event_key"), rs.getObject("captured_at_utc", OffsetDateTime.class), rs.getObject("received_at_utc", OffsetDateTime.class),
            rs.getString("metric"), (Double) rs.getObject("numeric_value"), rs.getString("text_value"), rs.getString("unit"),
            (Double) rs.getObject("latitude"), (Double) rs.getObject("longitude"), rs.getString("quality"), rs.getString("source"), rs.getString("metadata_json"));
    }
}
