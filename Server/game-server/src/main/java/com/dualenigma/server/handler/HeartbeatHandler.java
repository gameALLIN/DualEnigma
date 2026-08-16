package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageCodec;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.HeartbeatManager;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.s2c.S2C_HeartbeatAck;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 心跳处理器.
 * C2S_Heartbeat → 更新活跃时间 → 回复 S2C_HeartbeatAck
 */
@Component
public class HeartbeatHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final HeartbeatManager heartbeatManager;
    private final MessageCodec messageCodec;

    public HeartbeatHandler(MessageRouter messageRouter, HeartbeatManager heartbeatManager,
                            MessageCodec messageCodec) {
        this.messageRouter = messageRouter;
        this.heartbeatManager = heartbeatManager;
        this.messageCodec = messageCodec;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_HEARTBEAT, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        heartbeatManager.onHeartbeat(session);

        // 回复心跳确认（客户端据此计算应用层 RTT，HUD 显示 PING）
        S2C_HeartbeatAck ack = new S2C_HeartbeatAck();
        ack.setPlayerId(-1);
        ack.setTimestamp(System.currentTimeMillis());
        ack.getData().setServerTimestamp(System.currentTimeMillis());
        session.send(messageCodec.encode(ack));
    }
}
