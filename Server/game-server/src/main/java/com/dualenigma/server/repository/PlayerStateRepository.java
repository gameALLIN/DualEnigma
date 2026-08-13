package com.dualenigma.server.repository;

import com.dualenigma.server.entity.PlayerStateEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 玩家运行时状态表 Repository.
 */
@Repository
public interface PlayerStateRepository extends JpaRepository<PlayerStateEntity, Long> {

    List<PlayerStateEntity> findByRoomId(String roomId);
}
