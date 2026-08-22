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
 * 建筑操作处理器（预留：BuildingManager 未实现，schema 占位接线）.
 * C2S_BuildingPlace / C2S_BuildingRemove → room.onXxx（TODO）
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
        messageRouter.register(Envelope.BodyCase.BUILDING_PLACE, this);
        messageRouter.register(Envelope.BodyCase.BUILDING_REMOVE, this);
    }

    @Override
    public void handle(ClientSession session, Envelope env) {
        GameRoom room = roomManager.getRoom(session.getRoomCode());
        if (room == null) return;

        int playerId = session.getPlayerId();
        switch (env.getBodyCase()) {
            case BUILDING_PLACE -> room.onBuildingPlace(playerId,
                    env.getBuildingPlace().getBuildingType(),
                    env.getBuildingPlace().getMaterial(),
                    env.getBuildingPlace().getGridX(),
                    env.getBuildingPlace().getGridY());
            case BUILDING_REMOVE -> room.onBuildingRemove(playerId,
                    env.getBuildingRemove().getBuildingId());
            default -> { }
        }
    }
}
