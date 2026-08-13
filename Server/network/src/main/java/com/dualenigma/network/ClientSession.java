package com.dualenigma.network;

import org.springframework.web.socket.WebSocketSession;

/**
 * 客户端会话封装.
 * 封装 WebSocket Session，附加 playerId、roomCode 等业务信息.
 */
public class ClientSession {

    private final WebSocketSession webSocketSession;
    private int playerId = -1;       // -1 = 未分配
    private String roomCode;
    private long lastActiveTime;

    public ClientSession(WebSocketSession webSocketSession) {
        this.webSocketSession = webSocketSession;
        this.lastActiveTime = System.currentTimeMillis();
    }

    public WebSocketSession getWebSocketSession() { return webSocketSession; }

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
