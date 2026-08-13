package com.dualenigma.server.repository;

import com.dualenigma.server.entity.TalentRecordEntity;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

/**
 * 天赋选择记录表 Repository.
 */
@Repository
public interface TalentRecordRepository extends JpaRepository<TalentRecordEntity, Long> {

    List<TalentRecordEntity> findByRoomId(String roomId);

    List<TalentRecordEntity> findByRoomIdAndPlayerId(String roomId, byte playerId);
}
