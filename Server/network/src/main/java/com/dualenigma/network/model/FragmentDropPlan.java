package com.dualenigma.network.model;

/**
 * 碎片掉落计划项.
 * 由 FragmentPlanner 在 Preview 阶段生成.
 */
public class FragmentDropPlan {

    private int fragmentId;
    private int type;          // 0=冰晶, 1=熔岩, 2=岩石
    private float posX;
    private float posY;
    private float dropTime;    // 相对于阶段开始的掉落时间（秒）
    private long seed;         // 碎片物理掉落随机种子

    public FragmentDropPlan() {}

    public FragmentDropPlan(int fragmentId, int type, float posX, float posY, float dropTime, long seed) {
        this.fragmentId = fragmentId;
        this.type = type;
        this.posX = posX;
        this.posY = posY;
        this.dropTime = dropTime;
        this.seed = seed;
    }

    public int getFragmentId() { return fragmentId; }
    public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
    public int getType() { return type; }
    public void setType(int type) { this.type = type; }
    public float getPosX() { return posX; }
    public void setPosX(float posX) { this.posX = posX; }
    public float getPosY() { return posY; }
    public void setPosY(float posY) { this.posY = posY; }
    public float getDropTime() { return dropTime; }
    public void setDropTime(float dropTime) { this.dropTime = dropTime; }
    public long getSeed() { return seed; }
    public void setSeed(long seed) { this.seed = seed; }
}
