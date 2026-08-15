package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 房主请求开始对局.
 *
 * data: {} (空)
 * 服务端校验：发送者必须是房主(playerId=0) 且房间满员，否则忽略.
 */
public class C2S_StartGame extends Message {

    private Object data = new Object();

    public Object getData() { return data; }
    public void setData(Object data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_START_GAME; }
}
