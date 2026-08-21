package com.dualenigma.network.protocol.c2s;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * C2S: 碎片接住请求.
 *
 * data: { "fragmentId": 42, "posX": 3.5, "posY": -1.2 }
 * posX/posY 为客户端碰撞瞬间碎片的世界坐标（同接几何判定依据）.
 */
public class C2S_FragmentCaught extends Message {

    private FragmentCaughtData data = new FragmentCaughtData();

    public static class FragmentCaughtData {
        private int fragmentId;
        private float posX;
        private float posY;

        public int getFragmentId() { return fragmentId; }
        public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
        public float getPosX() { return posX; }
        public void setPosX(float posX) { this.posX = posX; }
        public float getPosY() { return posY; }
        public void setPosY(float posY) { this.posY = posY; }
    }

    public FragmentCaughtData getData() { return data; }
    public void setData(FragmentCaughtData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.C2S_FRAGMENT_CAUGHT; }
}
