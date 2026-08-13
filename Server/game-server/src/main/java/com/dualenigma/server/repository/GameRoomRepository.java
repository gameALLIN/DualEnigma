package com.dualenigma.server.repository;

import com.dualenigma.server.entity.GameRoomEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 房间表 Repository.
 */
@Repository
public interface GameRoomRepository extends JpaRepository<GameRoomEntity, String> {

    List<GameRoomEntity> findByStatus(GameRoomEntity.RoomStatus status);
}
