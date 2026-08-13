package com.dualenigma.server.entity;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * 对局结算表实体.
 */
@Entity
@Table(name = "game_result", indexes = {
    @Index(name = "idx_room", columnList = "room_id")
})
public class GameResultEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "room_id", nullable = false, length = 16)
    private String roomId;

    @Column(name = "is_victory", nullable = false)
    private boolean isVictory;

    @Column(name = "player0_alive", nullable = false)
    private boolean player0Alive;

    @Column(name = "player1_alive", nullable = false)
    private boolean player1Alive;

    @Column(name = "final_score", nullable = false)
    private int finalScore;

    @Column(name = "duration_sec", nullable = false)
    private int durationSec;

    @Column(name = "ended_at", nullable = false, updatable = false)
    private LocalDateTime endedAt = LocalDateTime.now();

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public String getRoomId() { return roomId; }
    public void setRoomId(String roomId) { this.roomId = roomId; }
    public boolean isVictory() { return isVictory; }
    public void setVictory(boolean victory) { isVictory = victory; }
    public boolean isPlayer0Alive() { return player0Alive; }
    public void setPlayer0Alive(boolean player0Alive) { this.player0Alive = player0Alive; }
    public boolean isPlayer1Alive() { return player1Alive; }
    public void setPlayer1Alive(boolean player1Alive) { this.player1Alive = player1Alive; }
    public int getFinalScore() { return finalScore; }
    public void setFinalScore(int finalScore) { this.finalScore = finalScore; }
    public int getDurationSec() { return durationSec; }
    public void setDurationSec(int durationSec) { this.durationSec = durationSec; }
    public LocalDateTime getEndedAt() { return endedAt; }
    public void setEndedAt(LocalDateTime endedAt) { this.endedAt = endedAt; }
}
