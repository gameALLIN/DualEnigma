package com.dualenigma.server.repository;

import com.dualenigma.server.entity.GameProgressEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

/**
 * 游戏进度表 Repository.
 */
@Repository
public interface GameProgressRepository extends JpaRepository<GameProgressEntity, Long> {

    Optional<GameProgressEntity> findByRoomId(String roomId);
}
