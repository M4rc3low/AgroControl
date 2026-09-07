package com.agrocontrol.telemetry;

import com.agrocontrol.telemetry.config.MqttProperties;
import com.agrocontrol.telemetry.config.TelemetrySecurityProperties;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;

@SpringBootApplication
@EnableConfigurationProperties({MqttProperties.class, TelemetrySecurityProperties.class})
public class TelemetryApplication {
    public static void main(String[] args) {
        SpringApplication.run(TelemetryApplication.class, args);
    }
}
