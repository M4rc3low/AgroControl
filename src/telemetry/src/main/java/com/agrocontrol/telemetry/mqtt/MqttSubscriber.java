package com.agrocontrol.telemetry.mqtt;

import com.agrocontrol.telemetry.api.TelemetryContracts.TelemetryEventRequest;
import com.agrocontrol.telemetry.config.MqttProperties;
import com.agrocontrol.telemetry.service.TelemetryIngestionService;
import java.nio.charset.StandardCharsets;
import java.util.UUID;
import java.util.regex.Pattern;
import org.eclipse.paho.client.mqttv3.IMqttDeliveryToken;
import org.eclipse.paho.client.mqttv3.MqttAsyncClient;
import org.eclipse.paho.client.mqttv3.MqttCallbackExtended;
import org.eclipse.paho.client.mqttv3.MqttConnectOptions;
import org.eclipse.paho.client.mqttv3.MqttMessage;
import org.eclipse.paho.client.mqttv3.persist.MemoryPersistence;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.SmartLifecycle;
import org.springframework.stereotype.Component;
import tools.jackson.databind.json.JsonMapper;

@Component
public class MqttSubscriber implements SmartLifecycle {
    private static final Logger log = LoggerFactory.getLogger(MqttSubscriber.class);
    private static final Pattern TOPIC = Pattern.compile("^agrocontrol/v1/devices/([0-9a-fA-F-]{36})/telemetry$");
    private final MqttProperties properties;
    private final TelemetryIngestionService ingestion;
    private final JsonMapper jsonMapper;
    private MqttAsyncClient client;
    private volatile boolean running;

    public MqttSubscriber(MqttProperties properties, TelemetryIngestionService ingestion, JsonMapper jsonMapper) {
        this.properties = properties;
        this.ingestion = ingestion;
        this.jsonMapper = jsonMapper;
    }

    @Override
    public void start() {
        if (!properties.enabled() || running) return;
        try {
            client = new MqttAsyncClient(properties.brokerUrl(), properties.clientId(), new MemoryPersistence());
            client.setCallback(new MqttCallbackExtended() {
                @Override public void connectComplete(boolean reconnect, String serverURI) { subscribe(); }
                @Override public void connectionLost(Throwable cause) { log.warn("MQTT connection lost: {}", cause == null ? "unknown" : cause.getMessage()); }
                @Override public void messageArrived(String topic, MqttMessage message) { handle(topic, message); }
                @Override public void deliveryComplete(IMqttDeliveryToken token) { }
            });
            var options = new MqttConnectOptions();
            options.setAutomaticReconnect(true);
            options.setCleanSession(false);
            options.setConnectionTimeout(10);
            client.connect(options).waitForCompletion(10_000);
            running = true;
        } catch (Exception ex) {
            log.error("Unable to start MQTT subscriber", ex);
        }
    }

    private void subscribe() {
        try {
            client.subscribe(properties.topicFilter(), Math.clamp(properties.qos(), 0, 2)).waitForCompletion(5_000);
            running = true;
            log.info("Subscribed to MQTT topic {}", properties.topicFilter());
        } catch (Exception ex) {
            log.error("MQTT subscription failed", ex);
        }
    }

    private void handle(String topic, MqttMessage message) {
        try {
            if (message.getPayload().length > properties.maxPayloadBytes()) throw new IllegalArgumentException("MQTT payload exceeds limit.");
            var matcher = TOPIC.matcher(topic);
            if (!matcher.matches()) throw new IllegalArgumentException("Unexpected MQTT topic.");
            var deviceId = UUID.fromString(matcher.group(1));
            var payload = new String(message.getPayload(), StandardCharsets.UTF_8);
            var request = jsonMapper.readValue(payload, TelemetryEventRequest.class);
            ingestion.ingestFromDevice(deviceId, request, "mqtt");
        } catch (Exception ex) {
            log.warn("Rejected MQTT telemetry message on {}: {}", topic, ex.getMessage());
        }
    }

    @Override public void stop() {
        running = false;
        try { if (client != null && client.isConnected()) client.disconnect().waitForCompletion(5_000); }
        catch (Exception ex) { log.warn("MQTT disconnect failed: {}", ex.getMessage()); }
        try { if (client != null) client.close(); } catch (Exception ignored) { }
    }
    @Override public boolean isRunning() { return running; }
    @Override public boolean isAutoStartup() { return true; }
    @Override public int getPhase() { return 0; }
}
