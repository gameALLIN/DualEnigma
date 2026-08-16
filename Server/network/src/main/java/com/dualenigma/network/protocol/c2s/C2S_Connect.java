package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 客户端连接请求.
 *
 * data: { "roomCode": "ABC123", "token": "jwt..." }
 *   roomCode 可选，为空则自动匹配
 *   token   可选，来自 account-server 登录；有效则注册在线状态（好友可见），
 *           无效/缺失按匿名处理（不影响进房）
 */
public class C2S_Connect extends Message {

    private ConnectData data = new ConnectData();

    public static class ConnectData {
        private String roomCode;
        private String token;

        public String getRoomCode() { return roomCode; }
        public void setRoomCode(String roomCode) { this.roomCode = roomCode; }

        public String getToken() { return token; }
        public void setToken(String token) { this.token = token; }
    }

    public ConnectData getData() { return data; }
    public void setData(ConnectData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_CONNECT; }
}
