package com.agrocontrol.telemetry.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "telemetry.mqtt")
public record MqttProperties(
    boolean enabled,
    String brokerUrl,
    String clientId,
    String topicFilter,
    int qos,
    int maxPayloadBytes
) {}
