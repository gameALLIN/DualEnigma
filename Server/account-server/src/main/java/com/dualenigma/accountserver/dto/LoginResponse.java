package com.dualenigma.accountserver.dto;

/**
 * 登录成功响应.
 */
public class LoginResponse {

    private String token;
    private long accountId;
    private String username;
    private String displayName;

    public LoginResponse(String token, long accountId, String username, String displayName) {
        this.token = token;
        this.accountId = accountId;
        this.username = username;
        this.displayName = displayName;
    }

    // --- Getters & Setters ---

    public String getToken() { return token; }
    public void setToken(String token) { this.token = token; }
    public long getAccountId() { return accountId; }
    public void setAccountId(long accountId) { this.accountId = accountId; }
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    public String getDisplayName() { return displayName; }
    public void setDisplayName(String displayName) { this.displayName = displayName; }
}
