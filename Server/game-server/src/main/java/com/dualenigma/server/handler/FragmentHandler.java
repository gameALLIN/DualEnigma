package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_FragmentCaught;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 碎片接住处理器.
 * C2S_FragmentCaught → ConflictResolver.onCatch()
 */
@Component
public class FragmentHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public FragmentHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_FRAGMENT_CAUGHT, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        C2S_FragmentCaught caught = (C2S_FragmentCaught) msg;
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room != null) {
            room.onFragmentCaught(session.getPlayerId(),
                    caught.getData().getFragmentId(),
                    caught.getData().getPosX(),
                    caught.getData().getPosY());
        }
    }
}
