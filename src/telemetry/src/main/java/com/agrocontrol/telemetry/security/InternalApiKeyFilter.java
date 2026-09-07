package com.agrocontrol.telemetry.security;

import com.agrocontrol.telemetry.config.TelemetrySecurityProperties;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

@Component
public class InternalApiKeyFilter extends OncePerRequestFilter {
    public static final String HEADER = "X-AgroControl-Internal-Key";
    private final byte[] expected;

    public InternalApiKeyFilter(TelemetrySecurityProperties properties) {
        if (properties.internalApiKey() == null || properties.internalApiKey().length() < 32) {
            throw new IllegalStateException("telemetry.internal-api-key must contain at least 32 characters");
        }
        this.expected = properties.internalApiKey().getBytes(StandardCharsets.UTF_8);
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request) {
        return !request.getRequestURI().startsWith("/api/v1/");
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
        throws ServletException, IOException {
        var provided = request.getHeader(HEADER);
        var accepted = provided != null && MessageDigest.isEqual(expected, provided.getBytes(StandardCharsets.UTF_8));
        if (!accepted) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
            response.setContentType("application/json");
            response.getWriter().write("{\"error\":\"invalid_internal_credentials\"}");
            return;
        }
        filterChain.doFilter(request, response);
    }
}
