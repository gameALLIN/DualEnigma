package com.dualenigma.server.entity;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * 房间表实体.
 */
@Entity
@Table(name = "game_room", indexes = {
    @Index(name = "idx_status", columnList = "status"),
    @Index(name = "idx_player0", columnList = "player0_id"),
    @Index(name = "idx_player1", columnList = "player1_id")
})
public class GameRoomEntity {

    @Id
    @Column(length = 16)
    private String id;  // roomCode

    @Column(name = "player0_id")
    private Long player0Id;

    @Column(name = "player1_id")
    private Long player1Id;

    @Enumerated(EnumType.STRING)
    private RoomStatus status = RoomStatus.waiting;

    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt = LocalDateTime.now();

    @Column(name = "started_at")
    private LocalDateTime startedAt;

    @Column(name = "ended_at")
    private LocalDateTime endedAt;

    public enum RoomStatus { waiting, playing, finished, abandoned }

    // --- Getters & Setters ---

    public String getId() { return id; }
    public void setId(String id) { this.id = id; }
    public Long getPlayer0Id() { return player0Id; }
    public void setPlayer0Id(Long player0Id) { this.player0Id = player0Id; }
    public Long getPlayer1Id() { return player1Id; }
    public void setPlayer1Id(Long player1Id) { this.player1Id = player1Id; }
    public RoomStatus getStatus() { return status; }
    public void setStatus(RoomStatus status) { this.status = status; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime createdAt) { this.createdAt = createdAt; }
    public LocalDateTime getStartedAt() { return startedAt; }
    public void setStartedAt(LocalDateTime startedAt) { this.startedAt = startedAt; }
    public LocalDateTime getEndedAt() { return endedAt; }
    public void setEndedAt(LocalDateTime endedAt) { this.endedAt = endedAt; }
}
