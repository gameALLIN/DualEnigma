package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 材料合成请求.
 *
 * data: { "fragmentIds": [42, 43, 44] }
 */
public class C2S_Synthesize extends Message {

    private SynthesizeData data = new SynthesizeData();

    public static class SynthesizeData {
        private int[] fragmentIds;

        public int[] getFragmentIds() { return fragmentIds; }
        public void setFragmentIds(int[] fragmentIds) { this.fragmentIds = fragmentIds; }
    }

    public SynthesizeData getData() { return data; }
    public void setData(SynthesizeData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_SYNTHESIZE; }
}
