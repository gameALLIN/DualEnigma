package com.dualenigma.accountserver.repository;

import com.dualenigma.accountserver.entity.PlayerAccount;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

/**
 * 账号表 Repository.
 */
@Repository
public interface PlayerAccountRepository extends JpaRepository<PlayerAccount, Long> {

    Optional<PlayerAccount> findByUsername(String username);

    boolean existsByUsername(String username);

    /** 按用户名/昵称模糊搜索（排除自己），用于添加好友。结果数由调用方通过 Pageable 限制 */
    @Query("SELECT a FROM PlayerAccount a WHERE a.id <> :selfId AND " +
           "(LOWER(a.username) LIKE LOWER(CONCAT('%', :keyword, '%')) OR " +
           "LOWER(a.displayName) LIKE LOWER(CONCAT('%', :keyword, '%'))) " +
           "ORDER BY a.username")
    Page<PlayerAccount> search(@Param("keyword") String keyword,
                               @Param("selfId") Long selfId,
                               Pageable pageable);
}
