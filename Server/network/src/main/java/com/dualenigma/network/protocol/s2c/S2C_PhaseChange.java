package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.GamePhase;
import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 阶段切换通知.
 *
 * data: { "phase": "FragmentCollect", "durationMs": 15000, "phaseEndTime": 1691908815200 }
 */
public class S2C_PhaseChange extends Message {

    private PhaseChangeData data = new PhaseChangeData();

    public static class PhaseChangeData {
        private GamePhase phase;
        private int durationMs;
        private long phaseEndTime;

        public GamePhase getPhase() { return phase; }
        public void setPhase(GamePhase phase) { this.phase = phase; }
        public int getDurationMs() { return durationMs; }
        public void setDurationMs(int durationMs) { this.durationMs = durationMs; }
        public long getPhaseEndTime() { return phaseEndTime; }
        public void setPhaseEndTime(long phaseEndTime) { this.phaseEndTime = phaseEndTime; }
    }

    public PhaseChangeData getData() { return data; }
    public void setData(PhaseChangeData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_PHASE_CHANGE; }
}
