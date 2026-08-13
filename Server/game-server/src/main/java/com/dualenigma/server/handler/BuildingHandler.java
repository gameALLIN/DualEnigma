package com.dualenigma.server.handler;

import com.dualenigma.network.ClientSession;
import com.dualenigma.network.MessageHandler;
import com.dualenigma.network.MessageRouter;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;
import com.dualenigma.network.protocol.c2s.C2S_BuildingPlace;
import com.dualenigma.network.protocol.c2s.C2S_BuildingRemove;
import com.dualenigma.server.game.GameRoom;
import com.dualenigma.server.game.RoomManager;
import org.springframework.stereotype.Component;

import jakarta.annotation.PostConstruct;

/**
 * 建筑操作处理器.
 * C2S_BuildingPlace / C2S_BuildingRemove → BuildingManager
 */
@Component
public class BuildingHandler implements MessageHandler {

    private final MessageRouter messageRouter;
    private final RoomManager roomManager;

    public BuildingHandler(MessageRouter messageRouter, RoomManager roomManager) {
        this.messageRouter = messageRouter;
        this.roomManager = roomManager;
    }

    @PostConstruct
    public void init() {
        messageRouter.register(MessageType.C2S_BUILDING_PLACE, this);
        messageRouter.register(MessageType.C2S_BUILDING_REMOVE, this);
    }

    @Override
    public void handle(ClientSession session, Message msg) {
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room == null) return;

        int playerId = session.getPlayerId();
        if (msg.getType() == MessageType.C2S_BUILDING_PLACE) {
            C2S_BuildingPlace place = (C2S_BuildingPlace) msg;
            room.onBuildingPlace(playerId, place.getData().getBuildingType(),
                    place.getData().getMaterial(), place.getData().getGridX(), place.getData().getGridY());
        } else if (msg.getType() == MessageType.C2S_BUILDING_REMOVE) {
            C2S_BuildingRemove remove = (C2S_BuildingRemove) msg;
            room.onBuildingRemove(playerId, remove.getData().getBuildingId());
        }
    }
}
