package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 灾难开始.
 *
 * data: { "disasterId": 101, "difficultyMultiplier": 0.8, "randomSeed": 987654321, "params": {...} }
 */
public class S2C_DisasterStart extends Message {

    private DisasterStartData data = new DisasterStartData();

    public static class DisasterParams {
        private String name;
        private float baseDPS;
        private float range;
        private float duration;

        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
        public float getBaseDPS() { return baseDPS; }
        public void setBaseDPS(float baseDPS) { this.baseDPS = baseDPS; }
        public float getRange() { return range; }
        public void setRange(float range) { this.range = range; }
        public float getDuration() { return duration; }
        public void setDuration(float duration) { this.duration = duration; }
    }

    public static class DisasterStartData {
        private int disasterId;
        private float difficultyMultiplier;
        private long randomSeed;
        private DisasterParams params;

        public int getDisasterId() { return disasterId; }
        public void setDisasterId(int disasterId) { this.disasterId = disasterId; }
        public float getDifficultyMultiplier() { return difficultyMultiplier; }
        public void setDifficultyMultiplier(float difficultyMultiplier) { this.difficultyMultiplier = difficultyMultiplier; }
        public long getRandomSeed() { return randomSeed; }
        public void setRandomSeed(long randomSeed) { this.randomSeed = randomSeed; }
        public DisasterParams getParams() { return params; }
        public void setParams(DisasterParams params) { this.params = params; }
    }

    public DisasterStartData getData() { return data; }
    public void setData(DisasterStartData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_DISASTER_START; }
}
