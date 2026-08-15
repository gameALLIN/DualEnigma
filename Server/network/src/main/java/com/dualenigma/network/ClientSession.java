package com.dualenigma.network;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;

import java.io.IOException;

/**
 * 客户端会话封装.
 * 封装 WebSocket Session，附加 playerId、roomCode 等业务信息.
 */
public class ClientSession {

    private static final Logger log = LoggerFactory.getLogger(ClientSession.class);

    private final WebSocketSession webSocketSession;
    private int playerId = -1;       // -1 = 未分配
    private String roomCode;
    private long lastActiveTime;

    public ClientSession(WebSocketSession webSocketSession) {
        this.webSocketSession = webSocketSession;
        this.lastActiveTime = System.currentTimeMillis();
    }

    public WebSocketSession getWebSocketSession() { return webSocketSession; }

    /**
     * 线程安全发送文本消息.
     * Tick 线程(10Hz 中频)与 WebSocket 消息线程(20Hz 转发)并发写同一会话，
     * 统一经此方法串行化，避免并发 sendMessage 异常。
     */
    public synchronized void send(String json) {
        try {
            if (isOpen()) {
                webSocketSession.sendMessage(new TextMessage(json));
            }
        } catch (IOException e) {
            log.warn("Failed to send to session {}: {}", getSessionId(), e.getMessage());
        }
    }

    public String getSessionId() { return webSocketSession.getId(); }

    public int getPlayerId() { return playerId; }
    public void setPlayerId(int playerId) { this.playerId = playerId; }

    public String getRoomCode() { return roomCode; }
    public void setRoomCode(String roomCode) { this.roomCode = roomCode; }

    public long getLastActiveTime() { return lastActiveTime; }
    public void updateLastActiveTime() { this.lastActiveTime = System.currentTimeMillis(); }

    public boolean isOpen() {
        return webSocketSession != null && webSocketSession.isOpen();
    }
}
