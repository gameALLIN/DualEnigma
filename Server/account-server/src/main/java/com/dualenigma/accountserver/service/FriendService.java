package com.dualenigma.accountserver.service;

import com.dualenigma.accountserver.dto.FriendInfo;
import com.dualenigma.accountserver.dto.FriendRequestInfo;
import com.dualenigma.accountserver.dto.InviteInfo;
import com.dualenigma.accountserver.entity.Friendship;
import com.dualenigma.accountserver.entity.PlayerAccount;
import com.dualenigma.accountserver.entity.RoomInvite;
import com.dualenigma.accountserver.repository.FriendshipRepository;
import com.dualenigma.accountserver.repository.PlayerAccountRepository;
import com.dualenigma.accountserver.repository.RoomInviteRepository;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/**
 * 好友与房间邀请服务.
 * 好友申请：发送 / 接受 / 拒绝 / 列表 / 删除.
 * 房间邀请：房主创建（携带 roomCode）/ 好友接受后拿 roomCode 连接游戏服.
 */
@Service
public class FriendService {

    private final FriendshipRepository friendshipRepository;
    private final RoomInviteRepository roomInviteRepository;
    private final PlayerAccountRepository accountRepository;

    public FriendService(FriendshipRepository friendshipRepository,
                         RoomInviteRepository roomInviteRepository,
                         PlayerAccountRepository accountRepository) {
        this.friendshipRepository = friendshipRepository;
        this.roomInviteRepository = roomInviteRepository;
        this.accountRepository = accountRepository;
    }

    // ============================================================
    //  好友申请
    // ============================================================

    /**
     * 发送好友申请（按用户名）.
     * @throws IllegalArgumentException 参数错误/已是好友/已有待处理申请
     */
    @Transactional
    public FriendRequestInfo sendRequest(Long requesterId, String username) {
        if (username == null || username.isBlank()) {
            throw new IllegalArgumentException("用户名不能为空");
        }

        PlayerAccount target = accountRepository.findByUsername(username.trim())
                .orElseThrow(() -> new IllegalArgumentException("用户不存在: " + username));

        if (target.getId().equals(requesterId)) {
            throw new IllegalArgumentException("不能添加自己为好友");
        }

        Optional<Friendship> existing = friendshipRepository.findBetween(requesterId, target.getId());
        if (existing.isPresent()) {
            Friendship f = existing.get();
            switch (f.getStatus()) {
                case ACCEPTED -> throw new IllegalArgumentException("你们已经是好友了");
                case PENDING -> throw new IllegalArgumentException(
                        f.getRequesterId().equals(requesterId)
                                ? "申请已发送，等待对方处理"
                                : "对方已向你发出申请，请先处理");
                case REJECTED -> {
                    // 拒绝过允许重新申请，复用原记录
                    f.setStatus(Friendship.Status.PENDING);
                    f.setRequesterId(requesterId);
                    f.setAddresseeId(target.getId());
                    f.setUpdatedAt(LocalDateTime.now());
                    friendshipRepository.save(f);
                    return toRequestInfo(f, requesterId);
                }
            }
        }

        Friendship friendship = new Friendship();
        friendship.setRequesterId(requesterId);
        friendship.setAddresseeId(target.getId());
        friendshipRepository.save(friendship);
        return toRequestInfo(friendship, requesterId);
    }

    /**
     * 我收到的好友申请列表（待处理）.
     */
    public List<FriendRequestInfo> getIncomingRequests(Long accountId) {
        List<FriendRequestInfo> result = new ArrayList<>();
        for (Friendship f : friendshipRepository
                .findByAddresseeIdAndStatus(accountId, Friendship.Status.PENDING)) {
            result.add(toRequestInfo(f, f.getRequesterId()));
        }
        return result;
    }

    /**
     * 接受好友申请（仅受方有效）.
     */
    @Transactional
    public void acceptRequest(Long accountId, Long requestId) {
        Friendship f = getOwnedRequest(accountId, requestId);
        f.setStatus(Friendship.Status.ACCEPTED);
        f.setUpdatedAt(LocalDateTime.now());
        friendshipRepository.save(f);
    }

    /**
     * 拒绝好友申请（仅受方有效）— 删除记录，允许日后重新申请.
     */
    @Transactional
    public void rejectRequest(Long accountId, Long requestId) {
        Friendship f = getOwnedRequest(accountId, requestId);
        friendshipRepository.delete(f);
    }

    /**
     * 我的好友列表.
     */
    public List<FriendInfo> listFriends(Long accountId) {
        List<FriendInfo> result = new ArrayList<>();
        for (Friendship f : friendshipRepository
                .findAcceptedInvolving(Friendship.Status.ACCEPTED, accountId)) {
            Long friendId = f.getRequesterId().equals(accountId)
                    ? f.getAddresseeId() : f.getRequesterId();
            accountRepository.findById(friendId).ifPresent(account -> {
                FriendInfo info = new FriendInfo();
                info.setAccountId(account.getId());
                info.setUsername(account.getUsername());
                info.setDisplayName(account.getDisplayName());
                info.setOnline(false); // TODO: 在线状态待接入 game-server 心跳查询
                result.add(info);
            });
        }
        return result;
    }

    /**
     * 删除好友（双向有效）.
     */
    @Transactional
    public void removeFriend(Long accountId, Long friendId) {
        Friendship f = friendshipRepository
                .findAcceptedBetween(Friendship.Status.ACCEPTED, accountId, friendId)
                .orElseThrow(() -> new IllegalArgumentException("对方不是你的好友"));
        friendshipRepository.delete(f);
    }

    /**
     * 搜索用户（添加好友用，最多返回 20 条）.
     */
    public List<FriendInfo> searchUsers(Long selfId, String keyword) {
        if (keyword == null || keyword.isBlank()) {
            return List.of();
        }
        Page<PlayerAccount> page = accountRepository.search(
                keyword.trim(), selfId, PageRequest.of(0, 20));
        List<FriendInfo> result = new ArrayList<>();
        for (PlayerAccount account : page.getContent()) {
            FriendInfo info = new FriendInfo();
            info.setAccountId(account.getId());
            info.setUsername(account.getUsername());
            info.setDisplayName(account.getDisplayName());
            result.add(info);
        }
        return result;
    }

    // ============================================================
    //  房间邀请
    // ============================================================

    /**
     * 房主创建邀请（必须是好友）.
     * 同一邀请人的旧待处理邀请自动作废，避免堆积.
     */
    @Transactional
    public InviteInfo createInvite(Long inviterId, Long friendId, String roomCode) {
        if (roomCode == null || roomCode.isBlank()) {
            throw new IllegalArgumentException("roomCode 不能为空");
        }
        friendshipRepository
                .findAcceptedBetween(Friendship.Status.ACCEPTED, inviterId, friendId)
                .orElseThrow(() -> new IllegalArgumentException("只能邀请好友"));

        roomInviteRepository.findByInviterIdAndStatus(inviterId, RoomInvite.Status.PENDING)
                .forEach(old -> {
                    old.setStatus(RoomInvite.Status.DECLINED);
                    roomInviteRepository.save(old);
                });

        RoomInvite invite = new RoomInvite();
        invite.setInviterId(inviterId);
        invite.setInviteeId(friendId);
        invite.setRoomCode(roomCode.trim());
        roomInviteRepository.save(invite);
        return toInviteInfo(invite, inviterId);
    }

    /**
     * 我收到的待处理邀请（过滤已过期）.
     */
    public List<InviteInfo> listInvites(Long inviteeId) {
        LocalDateTime expireBefore = LocalDateTime.now()
                .minusMinutes(RoomInvite.EXPIRE_MINUTES);
        List<InviteInfo> result = new ArrayList<>();
        for (RoomInvite invite : roomInviteRepository
                .findByInviteeIdAndStatus(inviteeId, RoomInvite.Status.PENDING)) {
            if (invite.getCreatedAt().isBefore(expireBefore)) {
                continue; // 过期邀请不再展示
            }
            result.add(toInviteInfo(invite, invite.getInviterId()));
        }
        return result;
    }

    /**
     * 接受邀请 → 返回 roomCode，客户端用它连接 game-server WebSocket.
     */
    @Transactional
    public String acceptInvite(Long inviteeId, Long inviteId) {
        RoomInvite invite = getOwnedInvite(inviteeId, inviteId);
        if (invite.getCreatedAt().isBefore(LocalDateTime.now()
                .minusMinutes(RoomInvite.EXPIRE_MINUTES))) {
            throw new IllegalArgumentException("邀请已过期");
        }
        invite.setStatus(RoomInvite.Status.ACCEPTED);
        roomInviteRepository.save(invite);
        return invite.getRoomCode();
    }

    /**
     * 拒绝邀请.
     */
    @Transactional
    public void declineInvite(Long inviteeId, Long inviteId) {
        RoomInvite invite = getOwnedInvite(inviteeId, inviteId);
        invite.setStatus(RoomInvite.Status.DECLINED);
        roomInviteRepository.save(invite);
    }

    // ============================================================
    //  内部方法
    // ============================================================

    /** 校验申请归属（仅受方可操作），不满足抛异常 */
    private Friendship getOwnedRequest(Long accountId, Long requestId) {
        Friendship f = friendshipRepository.findById(requestId)
                .orElseThrow(() -> new IllegalArgumentException("申请不存在"));
        if (!f.getAddresseeId().equals(accountId)) {
            throw new IllegalArgumentException("无权处理该申请");
        }
        if (f.getStatus() != Friendship.Status.PENDING) {
            throw new IllegalArgumentException("该申请已处理过");
        }
        return f;
    }

    /** 校验邀请归属（仅受方可操作） */
    private RoomInvite getOwnedInvite(Long inviteeId, Long inviteId) {
        RoomInvite invite = roomInviteRepository.findById(inviteId)
                .orElseThrow(() -> new IllegalArgumentException("邀请不存在"));
        if (!invite.getInviteeId().equals(inviteeId)) {
            throw new IllegalArgumentException("无权处理该邀请");
        }
        if (invite.getStatus() != RoomInvite.Status.PENDING) {
            throw new IllegalArgumentException("该邀请已处理过");
        }
        return invite;
    }

    /** Friendship → FriendRequestInfo（fromAccountId 为发起方） */
    private FriendRequestInfo toRequestInfo(Friendship f, Long fromId) {
        FriendRequestInfo info = new FriendRequestInfo();
        info.setRequestId(f.getId());
        info.setFromAccountId(fromId);
        info.setCreatedAt(f.getCreatedAt());
        accountRepository.findById(fromId).ifPresent(account -> {
            info.setFromUsername(account.getUsername());
            info.setFromDisplayName(account.getDisplayName());
        });
        return info;
    }

    /** RoomInvite → InviteInfo */
    private InviteInfo toInviteInfo(RoomInvite invite, Long fromId) {
        InviteInfo info = new InviteInfo();
        info.setInviteId(invite.getId());
        info.setFromAccountId(fromId);
        info.setRoomCode(invite.getRoomCode());
        info.setCreatedAt(invite.getCreatedAt());
        accountRepository.findById(fromId)
                .ifPresent(account -> info.setFromDisplayName(account.getDisplayName()));
        return info;
    }
}
