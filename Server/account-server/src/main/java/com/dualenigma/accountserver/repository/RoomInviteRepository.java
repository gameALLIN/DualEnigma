package com.dualenigma.accountserver.repository;

import com.dualenigma.accountserver.entity.RoomInvite;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 房间邀请表 Repository.
 */
@Repository
public interface RoomInviteRepository extends JpaRepository<RoomInvite, Long> {

    /** 我收到的待处理邀请 */
    List<RoomInvite> findByInviteeIdAndStatus(Long inviteeId, RoomInvite.Status status);

    /** 我发出的待处理邀请（防重复） */
    List<RoomInvite> findByInviterIdAndStatus(Long inviterId, RoomInvite.Status status);
}
