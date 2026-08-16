package com.dualenigma.accountserver.dto;

/**
 * 好友信息 DTO.
 *
 * status 四态：offline 离线 / online 在线（主界面空闲）/ teaming 组队中（房间等待）/ ingame 游戏中
 */
public class FriendInfo {

    private Long accountId;
    private String username;
    private String displayName;
    private boolean online;
    private String status = "offline";

    public Long getAccountId() { return accountId; }
    public void setAccountId(Long accountId) { this.accountId = accountId; }
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    public String getDisplayName() { return displayName; }
    public void setDisplayName(String displayName) { this.displayName = displayName; }
    public boolean isOnline() { return online; }
    public void setOnline(boolean online) { this.online = online; }
    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }
}
