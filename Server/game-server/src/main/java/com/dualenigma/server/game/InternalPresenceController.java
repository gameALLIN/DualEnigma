package com.dualenigma.server.game;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Map;

/**
 * 内部在线状态查询端点（供 account-server 好友列表合并，Docker 内网访问）.
 *
 * GET /internal/presence?ids=1,2,3
 * → { "1": {"online":true,"inGame":false,"roomCode":"ABC123"}, ... }
 *   未出现在结果中的 id = 离线（无 game-server 会话）
 */
@RestController
@RequestMapping("/internal")
public class InternalPresenceController {

    private final OnlineRegistry onlineRegistry;

    public InternalPresenceController(OnlineRegistry onlineRegistry) {
        this.onlineRegistry = onlineRegistry;
    }

    @GetMapping("/presence")
    public Map<Long, Map<String, Object>> presence(@RequestParam("ids") String ids) {
        List<Long> idList = new java.util.ArrayList<>();
        if (ids != null && !ids.isBlank()) {
            for (String part : ids.split(",")) {
                try {
                    idList.add(Long.parseLong(part.trim()));
                } catch (NumberFormatException ignored) {
                    // 跳过非法 id
                }
            }
        }
        return onlineRegistry.query(idList);
    }
}
