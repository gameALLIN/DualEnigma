package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_Synthesize;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 材料合成处理器.
 * C2S_Synthesize → SynthesisValidator.validate()
 */
@Component
public class SynthesizeHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public SynthesizeHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_SYNTHESIZE, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        C2S_Synthesize synthesize = (C2S_Synthesize) msg;
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onSynthesize(session.getPlayerId(), synthesize.getData().getFragmentIds());
        }
    }
}
