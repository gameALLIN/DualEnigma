package com.dualenigma.network.model;

/**
 * 灾难参数配置.
 */
public class DisasterParams {

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
