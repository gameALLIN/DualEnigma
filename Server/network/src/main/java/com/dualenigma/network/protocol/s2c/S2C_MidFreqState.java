package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

import java.util.List;

/**
 * S2C: 中频状态同步 (10Hz).
 * 服务器权威 HP/能量/碎片同步.
 *
 * data: { "players": [{ "playerId": 0, "hp": 85, "shelterEnergy": 75, "carriedFragments": [0,0,1] }] }
 */
public class S2C_MidFreqState extends Message {

    private MidFreqData data = new MidFreqData();

    public static class PlayerMidFreq {
        private int playerId;
        private int hp;
        private int shelterEnergy;
        private int[] carriedFragments;

        public int getPlayerId() { return playerId; }
        public void setPlayerId(int playerId) { this.playerId = playerId; }
        public int getHp() { return hp; }
        public void setHp(int hp) { this.hp = hp; }
        public int getShelterEnergy() { return shelterEnergy; }
        public void setShelterEnergy(int shelterEnergy) { this.shelterEnergy = shelterEnergy; }
        public int[] getCarriedFragments() { return carriedFragments; }
        public void setCarriedFragments(int[] carriedFragments) { this.carriedFragments = carriedFragments; }
    }

    public static class MidFreqData {
        private List<PlayerMidFreq> players;

        public List<PlayerMidFreq> getPlayers() { return players; }
        public void setPlayers(List<PlayerMidFreq> players) { this.players = players; }
    }

    public MidFreqData getData() { return data; }
    public void setData(MidFreqData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_MID_FREQ_STATE; }
}
