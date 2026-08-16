package com.dualenigma.accountserver.controller;

import com.dualenigma.accountserver.service.AuthService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

/**
 * 内部服务间端点（Docker 内网访问，供 game-server 校验客户端 Token）.
 *
 * GET /internal/auth/validate?token=xxx
 * → 200 {"accountId": 123} 或 401
 */
@RestController
@RequestMapping("/internal")
public class InternalAuthController {

    private final AuthService authService;

    public InternalAuthController(AuthService authService) {
        this.authService = authService;
    }

    @GetMapping("/auth/validate")
    public ResponseEntity<?> validate(@RequestParam("token") String token) {
        Long accountId = authService.validateToken(token);
        if (accountId == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(Map.of("error", "invalid token"));
        }
        return ResponseEntity.ok(Map.of("accountId", accountId));
    }
}
