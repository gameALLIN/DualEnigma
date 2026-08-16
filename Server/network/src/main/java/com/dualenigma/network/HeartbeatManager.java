package com.dualenigma.network;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.PingMessage;
import org.springframework.web.socket.WebSocketSession;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CopyOnWriteArrayList;

/**
 * 心跳 + 断线检测管理器.
 *
 * 双重检测：
 * 1. WebSocket Ping/Pong（传输层）
 * 2. 应用层心跳（C2S_Heartbeat / S2C_HeartbeatAck）
 *
 * 超时判定：连续 5 秒未收到心跳 → 判定断线
 */
@Component
public class HeartbeatManager {

    private static final Logger log = LoggerFactory.getLogger(HeartbeatManager.class);

    private final Map<String, ClientSession> sessions = new ConcurrentHashMap<>();

    /** 会话断开监听（业务模块挂载：如 game-server 在线注册表注销） */
    public interface DisconnectListener {
        void onDisconnect(ClientSession session);
    }

    private final List<DisconnectListener> disconnectListeners = new CopyOnWriteArrayList<>();

    public void addDisconnectListener(DisconnectListener listener) {
        disconnectListeners.add(listener);
    }

    public void register(WebSocketSession session) {
        sessions.put(session.getId(), new ClientSession(session));
    }

    public void unregister(WebSocketSession session) {
        ClientSession clientSession = sessions.get(session.getId());
        if (clientSession != null) {
            removeSession(clientSession);
        }
    }

    public ClientSession getClientSession(String sessionId) {
        return sessions.get(sessionId);
    }

    public void onPong(WebSocketSession session) {
        ClientSession clientSession = sessions.get(session.getId());
        if (clientSession != null) {
            clientSession.updateLastActiveTime();
        }
    }

    /**
     * 应用层心跳收到，更新活跃时间.
     */
    public void onHeartbeat(ClientSession session) {
        if (session != null) {
            session.updateLastActiveTime();
        }
    }

    /**
     * 定时检测：每 1 秒发送 WebSocket Ping 并检查超时.
     */
    @Scheduled(fixedRate = 1000)
    public void checkTimeouts() {
        for (Map.Entry<String, ClientSession> entry : sessions.entrySet()) {
            ClientSession session = entry.getValue();
            if (!session.isOpen()) {
                // 兜底清扫（afterConnectionClosed 竞态遗漏时）：同样必须通知监听器，
                // 否则在线注册表残留（玩家永远显示"组队中/游戏中"）
                removeSession(session);
                continue;
            }

            // 发送 WebSocket Ping
            try {
                session.getWebSocketSession().sendMessage(new PingMessage());
            } catch (IOException e) {
                log.warn("Failed to send ping to {}: {}", entry.getKey(), e.getMessage());
            }

            // TODO: 检查应用层心跳超时（5s），触发断线处理
        }
    }

    /** 移除会话并通知断线监听器（unregister 与超时清扫共用） */
    private void removeSession(ClientSession session) {
        ClientSession removed = sessions.remove(session.getSessionId());
        if (removed != null) {
            for (DisconnectListener listener : disconnectListeners) {
                try {
                    listener.onDisconnect(removed);
                } catch (Exception e) {
                    log.warn("Disconnect listener failed: {}", e.getMessage());
                }
            }
        }
    }
}
