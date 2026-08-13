package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 天赋选择.
 *
 * data: { "talentId": 15 }
 */
public class C2S_TalentSelect extends Message {

    private TalentSelectData data = new TalentSelectData();

    public static class TalentSelectData {
        private int talentId;

        public int getTalentId() { return talentId; }
        public void setTalentId(int talentId) { this.talentId = talentId; }
    }

    public TalentSelectData getData() { return data; }
    public void setData(TalentSelectData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_TALENT_SELECT; }
}
