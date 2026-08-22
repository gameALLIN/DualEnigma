package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import com.dualenigma.v1.Envelope;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 材料合成处理器（预留：SynthesisValidator 未实现，schema 占位接线）.
 * C2S_Synthesize → room.onSynthesize（TODO）
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
        messageRouter.register(Envelope.BodyCase.SYNTHESIZE, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onSynthesize(session.getPlayerId(),
                    env.getSynthesize().getFragmentIdsList().stream().mapToInt(Integer::intValue).toArray());
        }
    }
}
