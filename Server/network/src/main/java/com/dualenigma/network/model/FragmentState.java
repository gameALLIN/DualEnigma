package com.dualenigma.network.model;

/**
 * 碎片运行时状态（内存模型）.
 */
public class FragmentState {

    private int fragmentId;
    private int fragmentType;    // 0=冰晶, 1=熔岩, 2=岩石
    private float posX;
    private float posY;
    private String state = "Falling";  // Falling / Collected / Despawned
    private float dropTime;
    private long seed;

    public int getFragmentId() { return fragmentId; }
    public void setFragmentId(int fragmentId) { this.fragmentId = fragmentId; }
    public int getFragmentType() { return fragmentType; }
    public void setFragmentType(int fragmentType) { this.fragmentType = fragmentType; }
    public float getPosX() { return posX; }
    public void setPosX(float posX) { this.posX = posX; }
    public float getPosY() { return posY; }
    public void setPosY(float posY) { this.posY = posY; }
    public String getState() { return state; }
    public void setState(String state) { this.state = state; }
    public float getDropTime() { return dropTime; }
    public void setDropTime(float dropTime) { this.dropTime = dropTime; }
    public long getSeed() { return seed; }
    public void setSeed(long seed) { this.seed = seed; }
}
