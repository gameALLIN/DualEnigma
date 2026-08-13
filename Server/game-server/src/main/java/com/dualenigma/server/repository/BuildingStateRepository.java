package com.dualenigma.server.repository;

import com.dualenigma.server.entity.BuildingStateEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 建筑状态表 Repository.
 */
@Repository
public interface BuildingStateRepository extends JpaRepository<BuildingStateEntity, Long> {

    List<BuildingStateEntity> findByRoomId(String roomId);
}
