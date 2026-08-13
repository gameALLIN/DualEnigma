package com.dualenigma.accountserver.controller;

import com.dualenigma.accountserver.dto.AccountInfo;
import com.dualenigma.accountserver.dto.LoginRequest;
import com.dualenigma.accountserver.dto.LoginResponse;
import com.dualenigma.accountserver.dto.RegisterRequest;
import com.dualenigma.accountserver.entity.PlayerAccount;
import com.dualenigma.accountserver.service.AccountService;
import com.dualenigma.accountserver.service.AuthService;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

/**
 * 认证 REST API.
 *
 * POST /api/auth/register  — 注册
 * POST /api/auth/login     — 登录
 * GET  /api/account/info   — 查询账号信息（需 Token）
 * PUT  /api/account/name   — 更新昵称（需 Token）
 */
@RestController
@RequestMapping("/api")
public class AuthController {

    private final AuthService authService;
    private final AccountService accountService;

    public AuthController(AuthService authService, AccountService accountService) {
        this.authService = authService;
        this.accountService = accountService;
    }

    /**
     * 注册.
     */
    @PostMapping("/auth/register")
    public ResponseEntity<?> register(@Valid @RequestBody RegisterRequest request) {
        PlayerAccount account = authService.register(
                request.getUsername(),
                request.getPassword(),
                request.getDisplayName()
        );

        if (account == null) {
            return ResponseEntity
                    .status(HttpStatus.CONFLICT)
                    .body(Map.of("error", "用户名已存在"));
        }

        String token = authService.login(request.getUsername(), request.getPassword());
        return ResponseEntity.ok(new LoginResponse(
                token, account.getId(), account.getUsername(), account.getDisplayName()
        ));
    }

    /**
     * 登录.
     */
    @PostMapping("/auth/login")
    public ResponseEntity<?> login(@Valid @RequestBody LoginRequest request) {
        String token = authService.login(request.getUsername(), request.getPassword());

        if (token == null) {
            return ResponseEntity
                    .status(HttpStatus.UNAUTHORIZED)
                    .body(Map.of("error", "用户名或密码错误"));
        }

        // 查询账号信息填充响应
        PlayerAccount account = authService.getAccountById(
                authService.validateToken(token)
        ).orElseThrow();

        return ResponseEntity.ok(new LoginResponse(
                token, account.getId(), account.getUsername(), account.getDisplayName()
        ));
    }

    /**
     * 查询账号信息（需 Token）.
     */
    @GetMapping("/account/info")
    public ResponseEntity<?> getAccountInfo(@RequestHeader("Authorization") String authHeader) {
        Long accountId = extractAccountIdFromHeader(authHeader);
        if (accountId == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(Map.of("error", "无效或过期的 Token"));
        }

        return accountService.findById(accountId)
                .map(account -> {
                    AccountInfo info = new AccountInfo();
                    info.setId(account.getId());
                    info.setUsername(account.getUsername());
                    info.setDisplayName(account.getDisplayName());
                    info.setCreatedAt(account.getCreatedAt().toString());
                    return ResponseEntity.ok((Object) info);
                })
                .orElse(ResponseEntity.notFound().build());
    }

    /**
     * 更新昵称（需 Token）.
     */
    @PutMapping("/account/name")
    public ResponseEntity<?> updateDisplayName(
            @RequestHeader("Authorization") String authHeader,
            @RequestBody Map<String, String> body) {

        Long accountId = extractAccountIdFromHeader(authHeader);
        if (accountId == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                    .body(Map.of("error", "无效或过期的 Token"));
        }

        String newName = body.get("displayName");
        if (newName == null || newName.isBlank()) {
            return ResponseEntity.badRequest()
                    .body(Map.of("error", "昵称不能为空"));
        }

        return accountService.updateDisplayName(accountId, newName)
                .map(account -> ResponseEntity.ok(Map.of(
                        "id", account.getId(),
                        "displayName", account.getDisplayName()
                )))
                .orElse(ResponseEntity.notFound().build());
    }

    /**
     * 从 Authorization Header 提取 accountId.
     */
    private Long extractAccountIdFromHeader(String authHeader) {
        if (authHeader == null || !authHeader.startsWith("Bearer ")) {
            return null;
        }
        String token = authHeader.substring(7);
        return authService.validateToken(token);
    }
}
