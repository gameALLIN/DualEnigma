package com.dualenigma.network;

import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.v1.Envelope;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.BinaryMessage;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.BinaryWebSocketHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.ByteBuffer;

/**
 * WebSocket 连接生命周期管理.
 * 端点: ws://{host}:8080/game
 * 帧格式：proto3 Envelope 二进制帧（oneof case 即路由键）.
 */
@Component
public class GameWebSocketHandler extends BinaryWebSocketHandler {

    private static final Logger log = LoggerFactory.getLogger(GameWebSocketHandler.class);

    private final MessageRouter messageRouter;
    private final HeartbeatManager heartbeatManager;
    private final RespSender respSender;

    public GameWebSocketHandler(MessageRouter messageRouter,
                                HeartbeatManager heartbeatManager,
                                RespSender respSender) {
        this.messageRouter = messageRouter;
        this.heartbeatManager = heartbeatManager;
        this.respSender = respSender;
    }

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        log.info("WebSocket connected: {}", session.getId());
        heartbeatManager.register(session);
    }

    @Override
    protected void handleBinaryMessage(WebSocketSession session, BinaryMessage message) {
        ByteBuffer buffer = message.getPayload();
        byte[] bytes = new byte[buffer.remaining()];
        buffer.get(bytes);

        ClientSession cs = heartbeatManager.getClientSession(session.getId());
        Envelope env = ProtoCodec.parse(bytes);
        if (env == null || env.getBodyCase() == Envelope.BodyCase.BODY_NOT_SET) {
            log.error("Malformed envelope from {}: {} bytes", session.getId(), bytes.length);
            if (cs != null) {
                respSender.reply(cs, 0, NetErrorCode.UNKNOWN_TYPE);   // 1002 语义保留
            }
            return;
        }
        messageRouter.route(cs, env);
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
