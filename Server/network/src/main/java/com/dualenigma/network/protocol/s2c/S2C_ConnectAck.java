package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 连接确认.
 *
 * data: { "playerId": 0, "roomCode": "ABC123" }
 */
public class S2C_ConnectAck extends Message {

    private ConnectAckData data = new ConnectAckData();

    public static class ConnectAckData {
        private int playerId;
        private String roomCode;

        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public String getRoomCode() { return roomCode; }
        public void setRoomCode(String roomCode) { this.roomCode = roomCode; }
    }

    public ConnectAckData getData() { return data; }
    public void setData(ConnectAckData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_CONNECT_ACK; }
}
