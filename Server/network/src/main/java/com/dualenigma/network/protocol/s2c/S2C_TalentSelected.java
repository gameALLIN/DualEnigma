package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 对方天赋选择通知.
 *
 * data: { "playerId": 0, "talentId": 15 }
 */
public class S2C_TalentSelected extends Message {

    private TalentSelectedData data = new TalentSelectedData();

    public static class TalentSelectedData {
        private int playerId;
        private int talentId;

        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public int getTalentId() { return talentId; }
        public void setTalentId(int talentId) { this.talentId = talentId; }
    }

    public TalentSelectedData getData() { return data; }
    public void setData(TalentSelectedData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_TALENT_SELECTED; }
}
