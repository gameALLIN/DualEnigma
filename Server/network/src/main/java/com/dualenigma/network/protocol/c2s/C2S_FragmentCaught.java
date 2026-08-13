package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 碎片接住请求.
 *
 * data: { "fragmentId": 42 }
 */
public class C2S_FragmentCaught extends Message {

    private FragmentCaughtData data = new FragmentCaughtData();

    public static class FragmentCaughtData {
        private int fragmentId;

        public int getFragmentId() { return fragmentId; }
        public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
    }

    public FragmentCaughtData getData() { return data; }
    public void setData(FragmentCaughtData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_FRAGMENT_CAUGHT; }
}
