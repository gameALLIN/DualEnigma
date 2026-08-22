package com.dualenigma.network;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.socket.BinaryMessage;
import org.springframework.web.socket.CloseStatus;
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
    private Long accountId;          // 账号 ID（C2S_Connect 携带 token 经 account-server 校验后填入；null = 匿名）

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
    /**
     * 线程安全发送二进制帧（proto Envelope）.
     * 与 close() 同为 synchronized——Tick 线程与消息线程并发写同一会话统一串行化.
     */
    public synchronized void send(byte[] payload) {
        try {
            if (isOpen()) {
                webSocketSession.sendMessage(new BinaryMessage(payload));
            }
        } catch (IOException e) {
            log.warn("Failed to send binary to session {}: {}", getSessionId(), e.getMessage());
        }
    }

    /**
     * 服务器主动关闭连接（回执拒绝码后调用；随后的 afterConnectionClosed 正常走注销链路）.
     * 与 send() 同为 synchronized，避免并发 close/send 竞态.
     */
    public synchronized void close() {
        try {
            if (isOpen()) {
                webSocketSession.close(CloseStatus.NORMAL.withReason("rejected"));
            }
        } catch (IOException e) {
            log.warn("Failed to close session {}: {}", getSessionId(), e.getMessage());
        }
    }

    public String getSessionId() { return webSocketSession.getId(); }

    public int getPlayerId() { return playerId; }
    public void setPlayerId(int playerId) { this.playerId = playerId; }

    public Long getAccountId() { return accountId; }
    public void setAccountId(Long accountId) { this.accountId = accountId; }

    public String getRoomCode() { return roomCode; }
    public void setRoomCode(String roomCode) { this.roomCode = roomCode; }

    public long getLastActiveTime() { return lastActiveTime; }
    public void updateLastActiveTime() { this.lastActiveTime = System.currentTimeMillis(); }

    public boolean isOpen() {
        return webSocketSession != null && webSocketSession.isOpen();
    }
}
