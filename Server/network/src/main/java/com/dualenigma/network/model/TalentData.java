package com.dualenigma.network.model;

/**
 * 天赋数据.
 */
public class TalentData {

    private int talentId;
    private int playerId;    // 持有者
    private String name;
    private String description;

    public int getTalentId() { return talentId; }
    public void setTalentId(int talentId) { this.talentId = talentId; }
    public int getPlayerId() { return playerId; }
    public void setPlayerId(int playerId) { this.playerId = playerId; }
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    public String getDescription() { return description; }
    public void setDescription(String description) { this.description = description; }
}
