package com.dualenigma.server.repository;

import com.dualenigma.server.entity.SkillStateEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 技能状态表 Repository.
 */
@Repository
public interface SkillStateRepository extends JpaRepository<SkillStateEntity, Long> {

    List<SkillStateEntity> findByRoomId(String roomId);
}
