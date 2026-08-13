package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.GamePhase;
import com.dualenigma.network.model.BuildingState;
import com.dualenigma.network.model.DisasterState;
import com.dualenigma.network.model.FragmentState;
import com.dualenigma.network.model.PlayerState;
import com.dualenigma.network.model.SkillState;
import com.dualenigma.network.model.TalentData;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

import java.util.List;

/**
 * S2C: 重连全量快照.
 * 断线重连后服务器发送，恢复客户端到与服务器完全一致的状态.
 */
public class S2C_ReconnectSnapshot extends Message {

    private SnapshotData data = new SnapshotData();

    public static class SnapshotData {
        // 游戏进度
        private int chapter;
        private int section;
        private int round;
        private GamePhase currentPhase;
        private long phaseEndTime;
        private int score;

        // 角色状态
        private List<PlayerState> players;

        // 建筑状态
        private List<BuildingState> buildings;

        // 碎片状态
        private List<FragmentState> fragments;

        // 天赋/技能
        private List<TalentData> talents;
        private List<SkillState> skills;

        // 灾难状态（仅灾害冲击阶段）
        private DisasterState disaster;

        // 快照时间戳
        private long snapshotTimestamp;

        // --- Getters & Setters ---

        public int getChapter() { return chapter; }
        public void setChapter(int chapter) { this.chapter = chapter; }
        public int getSection() { return section; }
        public void setSection(int section) { this.section = section; }
        public int getRound() { return round; }
        public void setRound(int round) { this.round = round; }
        public GamePhase getCurrentPhase() { return currentPhase; }
        public void setCurrentPhase(GamePhase currentPhase) { this.currentPhase = currentPhase; }
        public long getPhaseEndTime() { return phaseEndTime; }
        public void setPhaseEndTime(long phaseEndTime) { this.phaseEndTime = phaseEndTime; }
        public int getScore() { return score; }
        public void setScore(int score) { this.score = score; }
        public List<PlayerState> getPlayers() { return players; }
        public void setPlayers(List<PlayerState> players) { this.players = players; }
        public List<BuildingState> getBuildings() { return buildings; }
        public void setBuildings(List<BuildingState> buildings) { this.buildings = buildings; }
        public List<FragmentState> getFragments() { return fragments; }
        public void setFragments(List<FragmentState> fragments) { this.fragments = fragments; }
        public List<TalentData> getTalents() { return talents; }
        public void setTalents(List<TalentData> talents) { this.talents = talents; }
        public List<SkillState> getSkills() { return skills; }
        public void setSkills(List<SkillState> skills) { this.skills = skills; }
        public DisasterState getDisaster() { return disaster; }
        public void setDisaster(DisasterState disaster) { this.disaster = disaster; }
        public long getSnapshotTimestamp() { return snapshotTimestamp; }
        public void setSnapshotTimestamp(long snapshotTimestamp) { this.snapshotTimestamp = snapshotTimestamp; }
    }

    public SnapshotData getData() { return data; }
    public void setData(SnapshotData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_RECONNECT_SNAPSHOT; }
}
