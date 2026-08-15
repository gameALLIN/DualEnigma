package com.dualenigma.accountserver.dto;

/**
 * 好友信息 DTO.
 */
public class FriendInfo {

    private Long accountId;
    private String username;
    private String displayName;
    private boolean online;

    public Long getAccountId() { return accountId; }
    public void setAccountId(Long accountId) { this.accountId = accountId; }
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    public String getDisplayName() { return displayName; }
    public void setDisplayName(String displayName) { this.displayName = displayName; }
    public boolean isOnline() { return online; }
    public void setOnline(boolean online) { this.online = online; }
}
