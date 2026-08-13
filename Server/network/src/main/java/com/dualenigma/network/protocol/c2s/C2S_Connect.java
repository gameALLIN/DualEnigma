package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 客户端连接请求.
 *
 * data: { "roomCode": "ABC123" }  // roomCode 可选，为空则自动匹配
 */
public class C2S_Connect extends Message {

    private ConnectData data = new ConnectData();

    public static class ConnectData {
        private String roomCode;

        public String getRoomCode() { return roomCode; }
        public void setRoomCode(String roomCode) { this.roomCode = roomCode; }
    }

    public ConnectData getData() { return data; }
    public void setData(ConnectData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_CONNECT; }
}
