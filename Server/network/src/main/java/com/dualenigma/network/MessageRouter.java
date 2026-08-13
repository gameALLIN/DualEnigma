package com.dualenigma.network;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.EnumMap;
import java.util.Map;

/**
 * 消息路由分发器.
 * 根据 MessageType 分发到对应的 MessageHandler.
 */
@Component
public class MessageRouter {

    private static final Logger log = LoggerFactory.getLogger(MessageRouter.class);

    private final Map<MessageType, MessageHandler> handlers = new EnumMap<>(MessageType.class);

    public void register(MessageType type, MessageHandler handler) {
        handlers.put(type, handler);
    }

    public void route(ClientSession session, Message msg) {
        if (msg == null || msg.getType() == null) {
            log.warn("Received null message or message with null type");
            return;
        }

        MessageHandler handler = handlers.get(msg.getType());
        if (handler != null) {
            handler.handle(session, msg);
        } else {
            log.warn("No handler registered for message type: {}", msg.getType());
        }
    }
}
