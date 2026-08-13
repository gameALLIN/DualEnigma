package com.dualenigma.network.model;

/**
 * 灾难运行时状态（内存模型）.
 */
public class DisasterState {

    private int disasterId;
    private float difficultyMult;
    private long randomSeed;
    private float elapsedTime;
    private boolean active;
    private DisasterParams params;

    public int getDisasterId() { return disasterId; }
    public void setDisasterId(int disasterId) { this.disasterId = disasterId; }
    public float getDifficultyMult() { return difficultyMult; }
    public void setDifficultyMult(float difficultyMult) { this.difficultyMult = difficultyMult; }
    public long getRandomSeed() { return randomSeed; }
    public void setRandomSeed(long randomSeed) { this.randomSeed = randomSeed; }
    public float getElapsedTime() { return elapsedTime; }
    public void setElapsedTime(float elapsedTime) { this.elapsedTime = elapsedTime; }
    public boolean isActive() { return active; }
    public void setActive(boolean active) { this.active = active; }
    public DisasterParams getParams() { return params; }
    public void setParams(DisasterParams params) { this.params = params; }
}
