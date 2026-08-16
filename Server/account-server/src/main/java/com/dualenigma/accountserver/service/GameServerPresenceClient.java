package com.dualenigma.accountserver.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.net.URI;
import java.time.Duration;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

/**
 * game-server 在线状态查询客户端（服务间调用）.
 *
 * GET {gameServerUrl}/internal/presence?ids=1,2,3
 * → { "1": {"online":true,"inGame":false,"roomCode":"ABC123"} }
 *
 * 容错：超时（800ms）/不可达 → 返回空 Map（降级为仅按本地 REST 活跃判定在线）。
 */
@Service
public class GameServerPresenceClient {

    private static final Logger log = LoggerFactory.getLogger(GameServerPresenceClient.class);

    private final HttpClient httpClient = HttpClient.newBuilder()
            .connectTimeout(Duration.ofMillis(800))
            .build();

    private final ObjectMapper objectMapper = new ObjectMapper();

    @Value("${dualenigma.game-server-url:http://localhost:8080}")
    private String gameServerUrl;

    /**
     * 查询一批账号在 game-server 的会话状态.
     * @return accountId → {online, inGame, roomCode}；仅含在线账号；失败返回空 Map
     */
    public Map<Long, Map<String, Object>> queryPresence(List<Long> accountIds) {
        if (accountIds == null || accountIds.isEmpty()) {
            return Map.of();
        }

        try {
            StringBuilder ids = new StringBuilder();
            for (Long id : accountIds) {
                if (ids.length() > 0) ids.append(',');
                ids.append(id);
            }

            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(gameServerUrl + "/internal/presence?ids=" + ids))
                    .timeout(Duration.ofMillis(800))
                    .GET()
                    .build();

            HttpResponse<String> response =
                    httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200) {
                log.warn("Presence query failed: status={}", response.statusCode());
                return Map.of();
            }

            Map<Long, Map<String, Object>> result = new HashMap<>();
            JsonNode root = objectMapper.readTree(response.body());
            Iterator<Map.Entry<String, JsonNode>> fields = root.fields();
            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> field = fields.next();
                try {
                    long accountId = Long.parseLong(field.getKey());
                    JsonNode node = field.getValue();
                    Map<String, Object> entry = new HashMap<>();
                    entry.put("online", node.path("online").asBoolean(false));
                    entry.put("inGame", node.path("inGame").asBoolean(false));
                    entry.put("roomCode", node.path("roomCode").asText(null));
                    result.put(accountId, entry);
                } catch (NumberFormatException ignored) {
                    // 跳过非法 key
                }
            }
            return result;
        } catch (Exception e) {
            log.warn("Presence query failed (game-server unreachable?): {}", e.getMessage());
            return Map.of();
        }
    }
}
