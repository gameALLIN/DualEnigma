package com.dualenigma.accountserver.controller;

import com.dualenigma.accountserver.dto.FriendInfo;
import com.dualenigma.accountserver.dto.FriendRequestInfo;
import com.dualenigma.accountserver.dto.InviteInfo;
import com.dualenigma.accountserver.service.AuthService;
import com.dualenigma.accountserver.service.FriendService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

/**
 * 好友与房间邀请 REST API（均需 Bearer Token）.
 *
 * 好友:
 *   POST   /api/friends/requests             发送好友申请 {username}
 *   GET    /api/friends/requests             我收到的待处理申请
 *   PUT    /api/friends/requests/{id}/accept 接受申请
 *   PUT    /api/friends/requests/{id}/reject 拒绝申请
 *   GET    /api/friends                      好友列表
 *   DELETE /api/friends/{friendId}           删除好友
 *   GET    /api/friends/search?keyword=      搜索用户
 *
 * 房间邀请:
 *   POST   /api/invites                      创建邀请 {friendId, roomCode}
 *   GET    /api/invites                      我收到的待处理邀请
 *   PUT    /api/invites/{id}/accept          接受 → 返回 {roomCode}
 *   PUT    /api/invites/{id}/decline         拒绝
 */
@RestController
@RequestMapping("/api")
public class FriendController {

    private final FriendService friendService;
    private final AuthService authService;

    public FriendController(FriendService friendService, AuthService authService) {
        this.friendService = friendService;
        this.authService = authService;
    }

    // ============================================================
    //  好友
    // ============================================================

    /**
     * 发送好友申请.
     */
    @PostMapping("/friends/requests")
    public ResponseEntity<?> sendRequest(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                         @RequestBody Map<String, String> body) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            FriendRequestInfo info = friendService.sendRequest(accountId, body.get("username"));
            return ResponseEntity.ok(info);
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    /**
     * 我收到的好友申请.
     */
    @GetMapping("/friends/requests")
    public ResponseEntity<?> getRequests(@RequestHeader(value = "Authorization", required = false) String authHeader) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        List<FriendRequestInfo> requests = friendService.getIncomingRequests(accountId);
        return ResponseEntity.ok(requests);
    }

    /**
     * 接受好友申请.
     */
    @PutMapping("/friends/requests/{id}/accept")
    public ResponseEntity<?> acceptRequest(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                           @PathVariable Long id) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            friendService.acceptRequest(accountId, id);
            return ResponseEntity.ok(Map.of("result", "accepted"));
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    /**
     * 拒绝好友申请.
     */
    @PutMapping("/friends/requests/{id}/reject")
    public ResponseEntity<?> rejectRequest(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                           @PathVariable Long id) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            friendService.rejectRequest(accountId, id);
            return ResponseEntity.ok(Map.of("result", "rejected"));
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    /**
     * 好友列表.
     */
    @GetMapping("/friends")
    public ResponseEntity<?> listFriends(@RequestHeader(value = "Authorization", required = false) String authHeader) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        List<FriendInfo> friends = friendService.listFriends(accountId);
        return ResponseEntity.ok(friends);
    }

    /**
     * 删除好友.
     */
    @DeleteMapping("/friends/{friendId}")
    public ResponseEntity<?> removeFriend(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                          @PathVariable Long friendId) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            friendService.removeFriend(accountId, friendId);
            return ResponseEntity.ok(Map.of("result", "removed"));
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    /**
     * 搜索用户（添加好友用）.
     */
    @GetMapping("/friends/search")
    public ResponseEntity<?> searchUsers(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                         @RequestParam(required = false) String keyword) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        List<FriendInfo> users = friendService.searchUsers(accountId, keyword);
        return ResponseEntity.ok(users);
    }

    // ============================================================
    //  房间邀请
    // ============================================================

    /**
     * 创建房间邀请（好友开房后把 roomCode 发给好友）.
     */
    @PostMapping("/invites")
    public ResponseEntity<?> createInvite(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                          @RequestBody Map<String, Object> body) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            Object friendIdObj = body.get("friendId");
            Object roomCodeObj = body.get("roomCode");
            if (friendIdObj == null || roomCodeObj == null) {
                return badRequest("friendId 和 roomCode 不能为空");
            }
            Long friendId = Long.valueOf(String.valueOf(friendIdObj));
            String roomCode = String.valueOf(roomCodeObj);
            InviteInfo info = friendService.createInvite(accountId, friendId, roomCode);
            return ResponseEntity.ok(info);
        } catch (NumberFormatException e) {
            return badRequest("friendId 格式错误");
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    /**
     * 我收到的待处理房间邀请.
     */
    @GetMapping("/invites")
    public ResponseEntity<?> listInvites(@RequestHeader(value = "Authorization", required = false) String authHeader) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        List<InviteInfo> invites = friendService.listInvites(accountId);
        return ResponseEntity.ok(invites);
    }

    /**
     * 接受邀请 → 客户端拿 roomCode 连接 game-server.
     */
    @PutMapping("/invites/{id}/accept")
    public ResponseEntity<?> acceptInvite(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                          @PathVariable Long id) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            String roomCode = friendService.acceptInvite(accountId, id);
            return ResponseEntity.ok(Map.of("roomCode", roomCode));
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    /**
     * 拒绝邀请.
     */
    @PutMapping("/invites/{id}/decline")
    public ResponseEntity<?> declineInvite(@RequestHeader(value = "Authorization", required = false) String authHeader,
                                           @PathVariable Long id) {
        Long accountId = extractAccountId(authHeader);
        if (accountId == null) {
            return unauthorized();
        }

        try {
            friendService.declineInvite(accountId, id);
            return ResponseEntity.ok(Map.of("result", "declined"));
        } catch (IllegalArgumentException e) {
            return badRequest(e.getMessage());
        }
    }

    // ============================================================
    //  内部方法
    // ============================================================

    /** 从 Authorization Header 提取 accountId，无效返回 null */
    private Long extractAccountId(String authHeader) {
        if (authHeader == null || !authHeader.startsWith("Bearer ")) {
            return null;
        }
        return authService.validateToken(authHeader.substring(7));
    }

    private ResponseEntity<?> unauthorized() {
        return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                .body(Map.of("error", "无效或过期的 Token"));
    }

    private ResponseEntity<?> badRequest(String message) {
        return ResponseEntity.badRequest().body(Map.of("error", message));
    }
}
