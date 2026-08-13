package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 灾难结束.
 *
 * data: {} (空)
 */
public class S2C_DisasterEnd extends Message {

    private Object data = new Object();

    public Object getData() { return data; }
    public void setData(Object data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_DISASTER_END; }
}
