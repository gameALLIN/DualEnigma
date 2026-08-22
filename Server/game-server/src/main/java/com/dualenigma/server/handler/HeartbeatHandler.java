package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.HeartbeatManager;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_HeartbeatAck;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 心跳处理器.
 * C2S_Heartbeat → 更新活跃时间 → 回复 S2C_HeartbeatAck（proto 二进制帧）
 */
@Component
public class HeartbeatHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final HeartbeatManager heartbeatManager;

    public HeartbeatHandler(MessageRouter messageRouter, HeartbeatManager heartbeatManager) {
        this.messageRouter = messageRouter;
        this.heartbeatManager = heartbeatManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(Envelope.BodyCase.HEARTBEAT, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        heartbeatManager.onHeartbeat(session);

        // 回复心跳确认（客户端据此计算应用层 RTT，HUD 显示 PING）
        long now = System.currentTimeMillis();
        Envelope ack = Envelope.newBuilder()
                .setPlayerId(-1)
                .setTimestamp(now)
                .setHeartbeatAck(S2C_HeartbeatAck.newBuilder().setServerTimestamp(now))
                .build();
        session.send(ack.toByteArray());
    }
}
