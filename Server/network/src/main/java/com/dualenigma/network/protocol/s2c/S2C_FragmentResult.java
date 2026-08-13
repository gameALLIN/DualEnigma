package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 碎片接住判定结果.
 *
 * data: { "fragmentId": 42, "playerId": 0, "multiplier": 3, "isSimultaneous": true }
 */
public class S2C_FragmentResult extends Message {

    private FragmentResultData data = new FragmentResultData();

    public static class FragmentResultData {
        private int fragmentId;
        private int playerId;
        private int multiplier;
        private boolean isSimultaneous;

        public int getFragmentId() { return fragmentId; }
        public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public int getMultiplier() { return multiplier; }
        public void setMultiplier(int multiplier) { this.multiplier = multiplier; }
        public boolean isSimultaneous() { return isSimultaneous; }
        public void setSimultaneous(boolean simultaneous) { isSimultaneous = simultaneous; }
    }

    public FragmentResultData getData() { return data; }
    public void setData(FragmentResultData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_FRAGMENT_RESULT; }
}
