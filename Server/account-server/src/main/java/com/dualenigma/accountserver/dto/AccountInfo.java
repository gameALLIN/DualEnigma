package com.dualenigma.accountserver.dto;

/**
 * 账号信息（查询用）.
 */
public class AccountInfo {

    private long id;
    private String username;
    private String displayName;
    private String createdAt;

    // --- Getters & Setters ---

    public long getId() { return id; }
    public void setId(long id) { this.id = id; }
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    public String getDisplayName() { return displayName; }
    public void setDisplayName(String displayName) { this.displayName = displayName; }
    public String getCreatedAt() { return createdAt; }
    public void setCreatedAt(String createdAt) { this.createdAt = createdAt; }
}
