package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 玩家加入房间通知（广播给房间内全部玩家）.
 *
 * data: { "playerId": 1, "playerCount": 2 }
 */
public class S2C_PlayerJoined extends Message {

    private PlayerJoinedData data = new PlayerJoinedData();

    public static class PlayerJoinedData {
        private int playerId;
        private int playerCount;

        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public int getPlayerCount() { return playerCount; }
        public void setPlayerCount(int playerCount) { this.playerCount = playerCount; }
    }

    public PlayerJoinedData getData() { return data; }
    public void setData(PlayerJoinedData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_PLAYER_JOINED; }
}
