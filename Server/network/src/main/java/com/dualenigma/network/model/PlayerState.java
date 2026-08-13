package com.dualenigma.network.model;

/**
 * 玩家运行时状态（内存模型）.
 * 每局每玩家一份，运行时在内存中维护.
 */
public class PlayerState {

    private int playerId;            // 0=Aqua, 1=Ignis
    private int hp;
    private float shelterEnergy;
    private float posX;
    private float posY;
    private float velocityX;
    private float velocityY;
    private String animState = "Idle";
    private boolean facing = true;
    private int[] carriedFragments;  // 碎片类型列表
    private float bufferTimer;       // 庇护能量耗尽后的缓冲计时器
    private boolean buffering;       // 是否处于缓冲期

    public int getPlayerId() { return playerId; }
    public void setPlayerId(int playerId) { this.playerId = playerId; }
    public int getHp() { return hp; }
    public void setHp(int hp) { this.hp = hp; }
    public float getShelterEnergy() { return shelterEnergy; }
    public void setShelterEnergy(float shelterEnergy) { this.shelterEnergy = shelterEnergy; }
    public float getPosX() { return posX; }
    public void setPosX(float posX) { this.posX = posX; }
    public float getPosY() { return posY; }
    public void setPosY(float posY) { this.posY = posY; }
    public float getVelocityX() { return velocityX; }
    public void setVelocityX(float velocityX) { this.velocityX = velocityX; }
    public float getVelocityY() { return velocityY; }
    public void setVelocityY(float velocityY) { this.velocityY = velocityY; }
    public String getAnimState() { return animState; }
    public void setAnimState(String animState) { this.animState = animState; }
    public boolean isFacing() { return facing; }
    public void setFacing(boolean facing) { this.facing = facing; }
    public int[] getCarriedFragments() { return carriedFragments; }
    public void setCarriedFragments(int[] carriedFragments) { this.carriedFragments = carriedFragments; }
    public float getBufferTimer() { return bufferTimer; }
    public void setBufferTimer(float bufferTimer) { this.bufferTimer = bufferTimer; }
    public boolean isBuffering() { return buffering; }
    public void setBuffering(boolean buffering) { this.buffering = buffering; }
}
