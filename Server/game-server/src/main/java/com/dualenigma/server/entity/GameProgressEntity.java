package com.dualenigma.server.entity;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * 游戏进度表实体（每局一条，36 轮更新）.
 */
@Entity
@Table(name = "game_progress", indexes = {
    @Index(name = "idx_room", columnList = "room_id")
})
public class GameProgressEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(nullable = false)
    private int chapter = 1;

    @Column(nullable = false)
    private int section = 1;

    @Column(nullable = false)
    private int round = 1;

    @Column(name = "current_phase", nullable = false, length = 32)
    private String currentPhase = "Preview";

    @Column(name = "phase_end_time")
    private LocalDateTime phaseEndTime;

    private int score = 0;

    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt = LocalDateTime.now();

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public int getChapter() { return chapter; }
    public void setChapter(int chapter) { this.chapter = chapter; }
    public int getSection() { return section; }
    public void setSection(int section) { this.section = section; }
    public int getRound() { return round; }
    public void setRound(int round) { this.round = round; }
    public String getCurrentPhase() { return currentPhase; }
    public void setCurrentPhase(String currentPhase) { this.currentPhase = currentPhase; }
    public LocalDateTime getPhaseEndTime() { return phaseEndTime; }
    public void setPhaseEndTime(LocalDateTime phaseEndTime) { this.phaseEndTime = phaseEndTime; }
    public int getScore() { return score; }
    public void setScore(int score) { this.score = score; }
    public LocalDateTime getUpdatedAt() { return updatedAt; }
    public void setUpdatedAt(LocalDateTime updatedAt) { this.updatedAt = updatedAt; }
}
