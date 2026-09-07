CREATE TABLE IF NOT EXISTS telemetry_devices (
    id UUID PRIMARY KEY,
    organization_id UUID NOT NULL,
    external_key VARCHAR(120) NOT NULL,
    device_type VARCHAR(40) NOT NULL,
    machine_id UUID NULL,
    farm_id UUID NULL,
    field_id UUID NULL,
    status VARCHAR(20) NOT NULL,
    credential_fingerprint VARCHAR(160) NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT ux_telemetry_devices_org_external UNIQUE (organization_id, external_key)
);

CREATE INDEX IF NOT EXISTS ix_telemetry_devices_org ON telemetry_devices (organization_id, status);

CREATE TABLE IF NOT EXISTS telemetry_events (
    id UUID PRIMARY KEY,
    organization_id UUID NOT NULL,
    device_id UUID NOT NULL REFERENCES telemetry_devices(id),
    event_key VARCHAR(120) NULL,
    captured_at_utc TIMESTAMPTZ NOT NULL,
    received_at_utc TIMESTAMPTZ NOT NULL,
    metric VARCHAR(80) NOT NULL,
    numeric_value DOUBLE PRECISION NULL,
    text_value VARCHAR(500) NULL,
    unit VARCHAR(40) NOT NULL,
    latitude DOUBLE PRECISION NULL,
    longitude DOUBLE PRECISION NULL,
    quality VARCHAR(40) NULL,
    source VARCHAR(80) NOT NULL,
    metadata_json TEXT NULL,
    CONSTRAINT ck_telemetry_events_value CHECK ((numeric_value IS NOT NULL) <> (text_value IS NOT NULL)),
    CONSTRAINT ck_telemetry_events_coordinates CHECK ((latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_telemetry_events_device_event_key
    ON telemetry_events (device_id, event_key)
    WHERE event_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_telemetry_events_org_device_time
    ON telemetry_events (organization_id, device_id, captured_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_telemetry_events_device_metric_time
    ON telemetry_events (device_id, metric, captured_at_utc DESC);
