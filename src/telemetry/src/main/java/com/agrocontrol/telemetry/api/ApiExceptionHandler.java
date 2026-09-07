package com.agrocontrol.telemetry.api;

import com.agrocontrol.telemetry.service.DeviceService;
import java.util.Map;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class ApiExceptionHandler {
    @ExceptionHandler(DeviceService.ResourceNotFoundException.class)
    ResponseEntity<?> notFound(DeviceService.ResourceNotFoundException ex) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(Map.of("error", "not_found", "message", ex.getMessage()));
    }

    @ExceptionHandler(IllegalArgumentException.class)
    ResponseEntity<?> invalid(IllegalArgumentException ex) {
        return ResponseEntity.unprocessableEntity().body(Map.of("error", "validation", "message", ex.getMessage()));
    }

    @ExceptionHandler(IllegalStateException.class)
    ResponseEntity<?> conflict(IllegalStateException ex) {
        return ResponseEntity.status(HttpStatus.CONFLICT).body(Map.of("error", "conflict", "message", ex.getMessage()));
    }
}
