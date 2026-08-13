package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

import java.util.List;

/**
 * S2C: 天赋 3 选 1 选项推送.
 *
 * data: { "options": [{ "talentId": 15, "name": "坚固壁垒", "description": "建筑HP+20%" }] }
 */
public class S2C_TalentOptions extends Message {

    private TalentOptionsData data = new TalentOptionsData();

    public static class TalentOption {
        private int talentId;
        private String name;
        private String description;

        public int getTalentId() { return talentId; }
        public void setTalentId(int talentId) { this.talentId = talentId; }
        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
    }

    public static class TalentOptionsData {
        private List<TalentOption> options;

        public List<TalentOption> getOptions() { return options; }
        public void setOptions(List<TalentOption> options) { this.options = options; }
    }

    public TalentOptionsData getData() { return data; }
    public void setData(TalentOptionsData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_TALENT_OPTIONS; }
}
