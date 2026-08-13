package com.dualenigma.network.protocol.s2c;

import com.dualenigma.network.protocol.Message;
import com.dualenigma.network.protocol.MessageType;

/**
 * S2C: 游戏开始.
 *
 * data: { "chapter": 1, "section": 1, "round": 1 }
 */
public class S2C_GameStart extends Message {

    private GameStartData data = new GameStartData();

    public static class GameStartData {
        private int chapter;
        private int section;
        private int round;

        public int getChapter() { return chapter; }
        public void setChapter(int chapter) { this.chapter = chapter; }
        public int getSection() { return section; }
        public void setSection(int section) { this.section = section; }
        public int getRound() { return round; }
        public void setRound(int round) { this.round = round; }
    }

    public GameStartData getData() { return data; }
    public void setData(GameStartData data) { this.data = data; }

    @Override
    public MessageType getType() { return MessageType.S2C_GAME_START; }
}
