package com.dualenigma.network;

import com.dualenigma.network.protocol.Message;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.BinaryWebSocketHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * WebSocket 连接生命周期管理.
 * 端点: ws://{host}:8080/game
 */
@Component
public class GameWebSocketHandler extends BinaryWebSocketHandler {

    private static final Logger log = LoggerFactory.getLogger(GameWebSocketHandler.class);

    private final MessageRouter messageRouter;
    private final MessageCodec messageCodec;
    private final HeartbeatManager heartbeatManager;

    public GameWebSocketHandler(MessageRouter messageRouter,
                                MessageCodec messageCodec,
                                HeartbeatManager heartbeatManager) {
        this.messageRouter = messageRouter;
        this.messageCodec = messageCodec;
        this.heartbeatManager = heartbeatManager;
    }

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        log.info("WebSocket connected: {}", session.getId());
        heartbeatManager.register(session);
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) {
        try {
            Message msg = messageCodec.decode(message.getPayload());
            ClientSession clientSession = heartbeatManager.getClientSession(session.getId());
            messageRouter.route(clientSession, msg);
        } catch (Exception e) {
            log.error("Failed to handle message from {}: {}", session.getId(), e.getMessage(), e);
        }
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
        log.info("WebSocket closed: {} status: {}", session.getId(), status);
        heartbeatManager.unregister(session);
    }

    @Override
    protected void handlePongMessage(WebSocketSession session, org.springframework.web.socket.PongMessage message) {
        heartbeatManager.onPong(session);
    }
}
