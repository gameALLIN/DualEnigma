package com.dualenigma.server.repository;

import com.dualenigma.server.entity.GameResultEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

/**
 * 对局结算表 Repository.
 */
@Repository
public interface GameResultRepository extends JpaRepository<GameResultEntity, Long> {

    Optional<GameResultEntity> findByRoomId(String roomId);
}
