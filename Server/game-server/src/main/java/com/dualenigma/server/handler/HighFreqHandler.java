package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import com.dualenigma.v1.C2S_HighFreqState;
import com.dualenigma.v1.Envelope;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 高频状态处理器 (20Hz).
 * C2S_HighFreqState → 转发给对方客户端（回执豁免：R5 拍板项，单向流）.
 */
@Component
public class HighFreqHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public HighFreqHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(Envelope.BodyCase.HIGH_FREQ_STATE, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        C2S_HighFreqState state = env.getHighFreqState();
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.forwardHighFreqState(session.getPlayerId(), state);
        }
    }
}
