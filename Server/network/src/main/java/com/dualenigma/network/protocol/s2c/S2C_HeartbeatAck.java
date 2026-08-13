package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 心跳回应.
 *
 * data: { "serverTimestamp": 1691908800900 }
 */
public class S2C_HeartbeatAck extends Message {

    private HeartbeatAckData data = new HeartbeatAckData();

    public static class HeartbeatAckData {
        private long serverTimestamp;

        public long getServerTimestamp() { return serverTimestamp; }
        public void setServerTimestamp(long serverTimestamp) { this.serverTimestamp = serverTimestamp; }
    }

    public HeartbeatAckData getData() { return data; }
    public void setData(HeartbeatAckData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_HEARTBEAT_ACK; }
}
