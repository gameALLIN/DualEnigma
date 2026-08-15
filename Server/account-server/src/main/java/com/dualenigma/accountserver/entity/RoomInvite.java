package com.dualenigma.accountserver.entity;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * 房间邀请表实体.
 * 好友开房后，房主邀请好友加入指定 roomCode 的房间.
 * status: PENDING(待处理) / ACCEPTED(已接受) / DECLINED(已拒绝)
 */
@Entity
@Table(name = "room_invite", indexes = {
    @Index(name = "idx_ri_invitee", columnList = "invitee_id, status"),
    @Index(name = "idx_ri_inviter", columnList = "inviter_id, status")
})
public class RoomInvite {

    public enum Status { PENDING, ACCEPTED, DECLINED }

    /** 邀请有效期（分钟），过期后查询时自动过滤 */
    public static final int EXPIRE_MINUTES = 10;

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "inviter_id", nullable = false)
    private Long inviterId;

    @Column(name = "invitee_id", nullable = false)
    private Long inviteeId;

    @Column(name = "room_code", nullable = false, length = 16)
    private String roomCode;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 16)
    private Status status = Status.PENDING;

    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt = LocalDateTime.now();

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public Long getInviterId() { return inviterId; }
    public void setInviterId(Long inviterId) { this.inviterId = inviterId; }
    public Long getInviteeId() { return inviteeId; }
    public void setInviteeId(Long inviteeId) { this.inviteeId = inviteeId; }
    public String getRoomCode() { return roomCode; }
    public void setRoomCode(String roomCode) { this.roomCode = roomCode; }
    public Status getStatus() { return status; }
    public void setStatus(Status status) { this.status = status; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime createdAt) { this.createdAt = createdAt; }
}
