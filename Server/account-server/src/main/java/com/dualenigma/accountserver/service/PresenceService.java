package com.dualenigma.accountserver.service;

import org.springframework.stereotype.Service;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 账号在线追踪（account-server 侧）.
 *
 * 原理：客户端每 5 秒经 SocialNotifyService 轮询带 Token 的 REST 接口，
 * 每次鉴权成功即触碰该账号的最后活跃时间。
 * 判定：最后活跃距今 ≤ ONLINE_WINDOW_MS（15s，容忍 3 次轮询间隔）→ 在线（主界面空闲态）。
 *
 * 组队中/游戏中由 game-server 的 WS 会话提供（见 GameServerPresenceClient），
 * 优先级：ingame > teaming > online > offline。
 */
@Service
public class PresenceService {

    /** 在线判定窗口（毫秒） */
    private static final long ONLINE_WINDOW_MS = 15_000;

    private final Map<Long, Long> lastSeen = new ConcurrentHashMap<>();

    /** 鉴权成功时触碰（任何带 Token 的 REST 调用） */
    public void touch(Long accountId) {
        if (accountId != null) {
            lastSeen.put(accountId, System.currentTimeMillis());
        }
    }

    /** 是否在线（REST 活跃窗口内） */
    public boolean isOnline(Long accountId) {
        if (accountId == null) return false;
        Long ts = lastSeen.get(accountId);
        return ts != null && System.currentTimeMillis() - ts <= ONLINE_WINDOW_MS;
    }
}
