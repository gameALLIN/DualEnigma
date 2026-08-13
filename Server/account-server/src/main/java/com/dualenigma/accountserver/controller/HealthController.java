package com.dualenigma.accountserver.controller;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

/**
 * 健康检查端点.
 *
 * GET /          — 服务信息
 * GET /health    — 健康状态
 */
@RestController
public class HealthController {

    @GetMapping("/")
    public Map<String, Object> root() {
        return Map.of(
                "service", "DualEnigma Account Server",
                "version", "1.0.0",
                "status", "running",
                "endpoints", Map.of(
                        "register", "POST /api/auth/register",
                        "login", "POST /api/auth/login",
                        "accountInfo", "GET /api/account/info",
                        "updateName", "PUT /api/account/name"
                )
        );
    }

    @GetMapping("/health")
    public Map<String, Object> health() {
        return Map.of("status", "UP");
    }
}
