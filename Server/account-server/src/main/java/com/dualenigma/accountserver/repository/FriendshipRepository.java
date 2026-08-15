package com.dualenigma.accountserver.repository;

import com.dualenigma.accountserver.entity.Friendship;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

/**
 * 好友关系表 Repository.
 */
@Repository
public interface FriendshipRepository extends JpaRepository<Friendship, Long> {

    /** 查询两个账号之间任意方向的唯一关系记录 */
    @Query("SELECT f FROM Friendship f WHERE " +
           "(f.requesterId = :a AND f.addresseeId = :b) OR " +
           "(f.requesterId = :b AND f.addresseeId = :a)")
    Optional<Friendship> findBetween(@Param("a") Long a, @Param("b") Long b);

    /** 我收到的好友申请（待处理） */
    List<Friendship> findByAddresseeIdAndStatus(Long addresseeId, Friendship.Status status);

    /** 我发出的好友申请（用于防重复发送） */
    List<Friendship> findByRequesterIdAndStatus(Long requesterId, Friendship.Status status);

    /** 我的好友列表（任意方向、已接受） */
    @Query("SELECT f FROM Friendship f WHERE f.status = :status AND " +
           "(f.requesterId = :me OR f.addresseeId = :me)")
    List<Friendship> findAcceptedInvolving(@Param("status") Friendship.Status status,
                                           @Param("me") Long me);

    /** 与某账号的已接受关系（判断是否好友） */
    @Query("SELECT f FROM Friendship f WHERE f.status = :status AND " +
           "((f.requesterId = :a AND f.addresseeId = :b) OR " +
           "(f.requesterId = :b AND f.addresseeId = :a))")
    Optional<Friendship> findAcceptedBetween(@Param("status") Friendship.Status status,
                                             @Param("a") Long a, @Param("b") Long b);
}
