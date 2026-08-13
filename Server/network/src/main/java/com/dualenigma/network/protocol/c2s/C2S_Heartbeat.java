package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 心跳.
 *
 * data: {} (空)
 */
public class C2S_Heartbeat extends Message {

    private Object data = new Object();

    public Object getData() { return data; }
    public void setData(Object data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_HEARTBEAT; }
}
