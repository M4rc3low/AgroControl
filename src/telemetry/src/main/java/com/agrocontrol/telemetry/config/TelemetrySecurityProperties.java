package com.agrocontrol.telemetry.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "telemetry")
public record TelemetrySecurityProperties(String internalApiKey) {}
