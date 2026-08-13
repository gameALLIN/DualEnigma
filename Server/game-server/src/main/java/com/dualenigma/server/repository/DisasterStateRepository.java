package com.dualenigma.server.repository;

import com.dualenigma.server.entity.DisasterStateEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 灾难状态表 Repository.
 */
@Repository
public interface DisasterStateRepository extends JpaRepository<DisasterStateEntity, Long> {

    List<DisasterStateEntity> findByRoomId(String roomId);
}
