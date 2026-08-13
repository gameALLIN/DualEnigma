package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 对方断线通知.
 *
 * state: waiting / aiTakeover / timeout
 */
public class S2C_OpponentDisconnect extends Message {

    private DisconnectData data = new DisconnectData();

    public static class DisconnectData {
        private String state;   // waiting / aiTakeover / timeout

        public String getState() { return state; }
        public void setState(String state) { this.state = state; }
    }

    public DisconnectData getData() { return data; }
    public void setData(DisconnectData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_OPPONENT_DISCONNECT; }
}
