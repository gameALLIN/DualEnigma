package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_HighFreqState;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 高频状态处理器 (20Hz).
 * C2S_HighFreqState → 转发给对方客户端.
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
        messageRouter.register(MessageType.C2S_HIGH_FREQ_STATE, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        C2S_HighFreqState state = (C2S_HighFreqState) msg;
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.forwardHighFreqState(session.getPlayerId(), state);
        }
    }
}
