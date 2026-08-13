package com.dualenigma.server.entity;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * 天赋选择记录表实体.
 */
@Entity
@Table(name = "talent_record", indexes = {
    @Index(name = "idx_room_player", columnList = "room_id,player_id")
})
public class TalentRecordEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "player_id", nullable = false)
    private byte playerId;

    @Column(name = "talent_id", nullable = false)
    private int talentId;

    @Column(name = "global_round", nullable = false)
    private int globalRound;

    @Column(name = "selected_at", nullable = false, updatable = false)
    private LocalDateTime selectedAt = LocalDateTime.now();

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public byte getPlayerId() { return playerId; }
    public void setPlayerId(byte playerId) { this.playerId = playerId; }
    public int getTalentId() { return talentId; }
    public void setTalentId(int talentId) { this.talentId = talentId; }
    public int getGlobalRound() { return globalRound; }
    public void setGlobalRound(int globalRound) { this.globalRound = globalRound; }
    public LocalDateTime getSelectedAt() { return selectedAt; }
    public void setSelectedAt(LocalDateTime selectedAt) { this.selectedAt = selectedAt; }
}
