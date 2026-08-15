package com.dualenigma.accountserver.entity;

import jakarta.persistence.*;
import java.time.LocalDateTime;

/**
 * 好友关系表实体.
 * 一行表示一条申请/关系：requester 发起，addressee 接收.
 * status: PENDING(待处理) / ACCEPTED(已互为好友) / REJECTED(已拒绝)
 */
@Entity
@Table(name = "friendship", indexes = {
    @Index(name = "idx_fs_requester", columnList = "requester_id, status"),
    @Index(name = "idx_fs_addressee", columnList = "addressee_id, status")
}, uniqueConstraints = {
    @UniqueConstraint(name = "uk_fs_pair", columnNames = {"requester_id", "addressee_id"})
})
public class Friendship {

    public enum Status { PENDING, ACCEPTED, REJECTED }

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "requester_id", nullable = false)
    private Long requesterId;

    @Column(name = "addressee_id", nullable = false)
    private Long addresseeId;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 16)
    private Status status = Status.PENDING;

    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt = LocalDateTime.now();

    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt = LocalDateTime.now();

    // --- Getters & Setters ---

    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    public Long getRequesterId() { return requesterId; }
    public void setRequesterId(Long requesterId) { this.requesterId = requesterId; }
    public Long getAddresseeId() { return addresseeId; }
    public void setAddresseeId(Long addresseeId) { this.addresseeId = addresseeId; }
    public Status getStatus() { return status; }
    public void setStatus(Status status) { this.status = status; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime createdAt) { this.createdAt = createdAt; }
    public LocalDateTime getUpdatedAt() { return updatedAt; }
    public void setUpdatedAt(LocalDateTime updatedAt) { this.updatedAt = updatedAt; }
}
