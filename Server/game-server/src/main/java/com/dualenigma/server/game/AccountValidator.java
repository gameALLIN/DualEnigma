package com.dualenigma.server.game;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.net.URI;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;

/**
 * 账号身份校验器：拿客户端的 JWT 调 account-server 内部校验端点换取 accountId.
 *
 * 安全语义：校验失败/超时 → 返回 null（匿名会话，可正常进房但不出现在在线列表，
 * 无法伪造他人身份）。
 */
@Component
public class AccountValidator {

    private static final Logger log = LoggerFactory.getLogger(AccountValidator.class);

    private final HttpClient httpClient = HttpClient.newBuilder()
            .connectTimeout(Duration.ofMillis(1500))
            .build();

    private final ObjectMapper objectMapper = new ObjectMapper();

    @Value("${dualenigma.account-server-url:http://localhost:8081}")
    private String accountServerUrl;

    /**
     * 校验 Token.
     * @return accountId；无效/缺失/服务不可达 → null（匿名）
     */
    public Long validate(String token) {
        if (token == null || token.isBlank()) {
            return null;
        }

        try {
            String url = accountServerUrl + "/internal/auth/validate?token="
                    + URLEncoder.encode(token, StandardCharsets.UTF_8);
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(url))
                    .timeout(Duration.ofMillis(1500))
                    .GET()
                    .build();

            HttpResponse<String> response =
                    httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200) {
                log.warn("Token validate rejected by account-server: status={}", response.statusCode());
                return null;
            }

            JsonNode node = objectMapper.readTree(response.body());
            if (node.hasNonNull("accountId")) {
                return node.get("accountId").asLong();
            }
            return null;
        } catch (Exception e) {
            log.warn("Token validate failed (account-server unreachable?): {}", e.getMessage());
            return null;
        }
    }
}
