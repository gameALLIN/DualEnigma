package com.dualenigma.server.game;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.HeartbeatManager;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 在线注册表：accountId → 在线状态（所在房间 / 是否开局）.
 *
 * 数据源：WebSocket 会话生命周期.
 *   进房     → register（teaming）
 *   开局     → markInGame（ingame）
 *   断开     → remove（离线）
 *
 * 查询方：InternalPresenceController（account-server 的好友列表状态合并）.
 */
@Component
public class OnlineRegistry implements HeartbeatManager.DisconnectListener {

    private static final Logger log = LoggerFactory.getLogger(OnlineRegistry.class);

    private final HeartbeatManager heartbeatManager;

    public OnlineRegistry(HeartbeatManager heartbeatManager) {
        this.heartbeatManager = heartbeatManager;
    }

    @PostConstruct
    public void init() {
        heartbeatManager.addDisconnectListener(this);
    }

    /** 会话断开 → 注销在线（HeartbeatManager 回调） */
    @Override
    public void onDisconnect(ClientSession session) {
        if (session.getAccountId() != null) {
            remove(session.getAccountId());
        }
    }

    /** 在线状态条目 */
    public static class Presence {
        private final long accountId;
        private final String roomCode;
        private volatile boolean inGame;

        public Presence(long accountId, String roomCode) {
            this.accountId = accountId;
            this.roomCode = roomCode;
        }

        public long getAccountId() { return accountId; }
        public String getRoomCode() { return roomCode; }
        public boolean isInGame() { return inGame; }
        public void setInGame(boolean inGame) { this.inGame = inGame; }
    }

    private final Map<Long, Presence> online = new ConcurrentHashMap<>();

    /** 进房：注册在线（组队中） */
    public void register(long accountId, String roomCode) {
        online.put(accountId, new Presence(accountId, roomCode));
        log.debug("Presence register: accountId={}, room={}", accountId, roomCode);
    }

    /** 房间开局：房间内全部已识别账号标记游戏中 */
    public void markInGame(Iterable<Long> accountIds) {
        for (Long id : accountIds) {
            Presence p = online.get(id);
            if (p != null) {
                p.setInGame(true);
            }
        }
    }

    /** 断开：注销在线 */
    public void remove(long accountId) {
        online.remove(accountId);
        log.debug("Presence remove: accountId={}", accountId);
    }

    /**
     * 批量查询在线状态.
     * @return accountId → 状态 Map（仅包含在线的；离线的不出现在结果中）
     */
    public Map<Long, Map<String, Object>> query(List<Long> accountIds) {
        Map<Long, Map<String, Object>> result = new HashMap<>();
        for (Long id : accountIds) {
            if (id == null) continue;
            Presence p = online.get(id);
            if (p == null) continue;

            Map<String, Object> entry = new HashMap<>();
            entry.put("online", true);
            entry.put("inGame", p.isInGame());
            entry.put("roomCode", p.getRoomCode());
            result.put(id, entry);
        }
        return result;
    }

    /** 当前在线账号数（监控用） */
    public int onlineCount() {
        return online.size();
    }

    /** 收集房间内已识别账号 ID */
    public static List<Long> collectAccountIds(ClientSession[] players) {
        List<Long> ids = new ArrayList<>();
        if (players == null) return ids;
        for (ClientSession s : players) {
            if (s != null && s.getAccountId() != null) {
                ids.add(s.getAccountId());
            }
        }
        return ids;
    }
}
