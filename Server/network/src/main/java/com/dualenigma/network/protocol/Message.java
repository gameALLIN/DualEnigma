package com.dualenigma.network.protocol;

import com.fasterxml.jackson.annotation.JsonSubTypes;
import com.fasterxml.jackson.annotation.JsonTypeInfo;

/**
 * WebSocket 消息基类.
 *
 * 统一 JSON 结构：
 * { "type": "...", "timestamp": 0, "playerId": 0, "data": { ... } }
 */
@JsonTypeInfo(use = JsonTypeInfo.Id.NAME, property = "type")
@JsonSubTypes({
    // C2S
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_Connect.class, name = "C2S_Connect"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_HighFreqState.class, name = "C2S_HighFreqState"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_FragmentCaught.class, name = "C2S_FragmentCaught"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_BuildingPlace.class, name = "C2S_BuildingPlace"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_BuildingRemove.class, name = "C2S_BuildingRemove"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_Synthesize.class, name = "C2S_Synthesize"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_SkillActivate.class, name = "C2S_SkillActivate"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_TalentSelect.class, name = "C2S_TalentSelect"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.c2s.C2S_Heartbeat.class, name = "C2S_Heartbeat"),
    // S2C
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_ConnectAck.class, name = "S2C_ConnectAck"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_GameStart.class, name = "S2C_GameStart"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_PhaseChange.class, name = "S2C_PhaseChange"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_HighFreqState.class, name = "S2C_HighFreqState"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_MidFreqState.class, name = "S2C_MidFreqState"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_FragmentDropPlan.class, name = "S2C_FragmentDropPlan"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_FragmentResult.class, name = "S2C_FragmentResult"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_DisasterStart.class, name = "S2C_DisasterStart"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_DisasterEnd.class, name = "S2C_DisasterEnd"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_BuildingUpdate.class, name = "S2C_BuildingUpdate"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_SkillResult.class, name = "S2C_SkillResult"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_TalentOptions.class, name = "S2C_TalentOptions"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_TalentSelected.class, name = "S2C_TalentSelected"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_ReconnectSnapshot.class, name = "S2C_ReconnectSnapshot"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_OpponentDisconnect.class, name = "S2C_OpponentDisconnect"),
    @JsonSubTypes.Type(value = com.dualenigma.network.protocol.s2c.S2C_HeartbeatAck.class, name = "S2C_HeartbeatAck"),
})
public abstract class Message {

    private long timestamp;
    private int playerId;

    public long getTimestamp() { return timestamp; }
    public void setTimestamp(long timestamp) { this.timestamp = timestamp; }

    public int getPlayerId() { return playerId; }
    public void setPlayerId(int playerId) { this.playerId = playerId; }

    public abstract MessageType getType();
}
