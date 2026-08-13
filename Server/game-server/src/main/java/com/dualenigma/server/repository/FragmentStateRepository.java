package com.dualenigma.server.repository;

import com.dualenigma.server.entity.FragmentStateEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 碎片状态表 Repository.
 */
@Repository
public interface FragmentStateRepository extends JpaRepository<FragmentStateEntity, Long> {

    List<FragmentStateEntity> findByRoomId(String roomId);

    List<FragmentStateEntity> findByRoomIdAndState(String roomId, String state);
}
