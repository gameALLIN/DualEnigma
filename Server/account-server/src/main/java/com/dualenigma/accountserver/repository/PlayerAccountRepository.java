package com.dualenigma.accountserver.repository;

import com.dualenigma.accountserver.entity.PlayerAccount;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

/**
 * 账号表 Repository.
 */
@Repository
public interface PlayerAccountRepository extends JpaRepository<PlayerAccount, Long> {

    Optional<PlayerAccount> findByUsername(String username);

    boolean existsByUsername(String username);
}
