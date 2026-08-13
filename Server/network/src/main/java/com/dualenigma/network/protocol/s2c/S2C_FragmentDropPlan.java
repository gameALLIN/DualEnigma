package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

import java.util.List;

/**
 * S2C: 碎片掉落计划.
 * Preview 阶段下发，双方各自模拟物理掉落.
 *
 * data: { "plan": [{ "fragmentId": 0, "type": 0, "position": {"x":-5,"y":12}, "dropTime": 0.0, "seed": 12345 }] }
 */
public class S2C_FragmentDropPlan extends Message {

    private DropPlanData data = new DropPlanData();

    public static class Vec2 {
        private float x;
        private float y;

        public float getX() { return x; }
        public void setX(float x) { this.x = x; }
        public float getY() { return y; }
        public void setY(float y) { this.y = y; }
    }

    public static class FragmentDropItem {
        private int fragmentId;
        private int type;
        private Vec2 position;
        private float dropTime;
        private long seed;

        public int getFragmentId() { return fragmentId; }
        public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
        public int getType() { return type; }
        public void setType(int type) { this.type = type; }
        public Vec2 getPosition() { return position; }
        public void setPosition(Vec2 position) { this.position = position; }
        public float getDropTime() { return dropTime; }
        public void setDropTime(float dropTime) { this.dropTime = dropTime; }
        public long getSeed() { return seed; }
        public void setSeed(long seed) { this.seed = seed; }
    }

    public static class DropPlanData {
        private List<FragmentDropItem> plan;

        public List<FragmentDropItem> getPlan() { return plan; }
        public void setPlan(List<FragmentDropItem> plan) { this.plan = plan; }
    }

    public DropPlanData getData() { return data; }
    public void setData(DropPlanData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_FRAGMENT_DROP_PLAN; }
}
