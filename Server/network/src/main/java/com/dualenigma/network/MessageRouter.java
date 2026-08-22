package com.dualenigma.network;

import com.dualenigma.network.protocol.NetErrorCode;
import com.dualenigma.v1.Envelope;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.EnumMap;
import java.util.Map;

/**
 * 消息路由分发器.
 * 根据 Envelope.BodyCase 分发到对应的 MessageHandler（oneof case 即路由键）.
 */
@Component
public class MessageRouter {

    private static final Logger log = LoggerFactory.getLogger(MessageRouter.class);

    private final Map<Envelope.BodyCase, MessageHandler> handlers = new EnumMap<>(Envelope.BodyCase.class);
    private final RespSender respSender;

    public MessageRouter(RespSender respSender) {
        this.respSender = respSender;
    }

    public void register(Envelope.BodyCase type, MessageHandler handler) {
        handlers.put(type, handler);
    }

    public void route(ClientSession session, Envelope env) {
        if (env == null || env.getBodyCase() == null) {
            log.warn("Received null envelope or envelope with body not set");
            return;
        }

        MessageHandler handler = handlers.get(env.getBodyCase());
        if (handler != null) {
            handler.handle(session, env);
        } else {
            log.warn("No handler registered for message body case: {}", env.getBodyCase());
            respSender.reply(session, env.getReqId(), NetErrorCode.UNKNOWN_TYPE);
        }
    }
}
