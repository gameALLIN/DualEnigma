package com.dualenigma.network;

import com.dualenigma.v1.C2S_Connect;
import com.dualenigma.v1.C2S_FragmentCaught;
import com.dualenigma.v1.C2S_HighFreqState;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.GamePhasePb;
import com.dualenigma.v1.S2C_ConnectAck;
import com.dualenigma.v1.S2C_FragmentDropPlan;
import com.dualenigma.v1.S2C_FragmentResult;
import com.dualenigma.v1.S2C_GameStart;
import com.dualenigma.v1.S2C_HeartbeatAck;
import com.dualenigma.v1.S2C_MidFreqState;
import com.dualenigma.v1.S2C_OpponentDisconnect;
import com.dualenigma.v1.S2C_PhaseChange;
import com.dualenigma.v1.S2C_PlayerJoined;
import com.dualenigma.v1.S2C_Resp;
import com.dualenigma.v1.Vec2;
import com.google.protobuf.InvalidProtocolBufferException;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * PS-0 验收：16 种在用消息 round-trip（构造 → toByteArray → parseFrom → 字段断言）.
 * 不做 golden bytes（protoc 小版本可能改变编码细节）；坏字节断言抛 InvalidProtocolBufferException
 * （PS-1 的 ProtoCodec.parse 将其兜底为 null + 1002 回执）.
 */
class ProtoRoundTripTest {

    private static Envelope rt(Envelope env) throws InvalidProtocolBufferException {
        return Envelope.parseFrom(env.toByteArray());
    }

    /** 信封字段语义：timestamp / player_id / req_id 透传；body case 为路由键. */
    @Test
    void envelopeCarriesHeaderFields() throws Exception {
        Envelope env = Envelope.newBuilder()
                .setTimestamp(123456789L)
                .setPlayerId(-1)
                .setReqId(7)
                .setStartGame(com.dualenigma.v1.C2S_StartGame.getDefaultInstance())
                .build();
        Envelope parsed = rt(env);
        assertEquals(123456789L, parsed.getTimestamp());
        assertEquals(-1, parsed.getPlayerId());
        assertEquals(7, parsed.getReqId());
        assertEquals(Envelope.BodyCase.START_GAME, parsed.getBodyCase());
    }

    /** C2S_Connect：好友房码 + token；空 room_code = 自动匹配语义. */
    @Test
    void c2sConnectRoundTrip() throws Exception {
        Envelope env = Envelope.newBuilder().setConnect(
                C2S_Connect.newBuilder().setRoomCode("8F3K").setToken("jwt-token")).build();
        assertEquals("8F3K", rt(env).getConnect().getRoomCode());
        assertEquals("jwt-token", rt(env).getConnect().getToken());

        Envelope auto = Envelope.newBuilder().setConnect(C2S_Connect.getDefaultInstance()).build();
        assertEquals("", rt(auto).getConnect().getRoomCode());
    }

    /** C2S_HighFreqState：Vec2 + 动画/朝向/HP/能量. */
    @Test
    void c2sHighFreqRoundTrip() throws Exception {
        C2S_HighFreqState body = C2S_HighFreqState.newBuilder()
                .setPosition(Vec2.newBuilder().setX(1.5f).setY(-2.25f))
                .setVelocity(Vec2.newBuilder().setX(0f).setY(4.2f))
                .setAnimState("Jump")
                .setFacing(false)
                .setHp(87)
                .setShelterEnergy(66.5f)
                .build();
        C2S_HighFreqState parsed = rt(Envelope.newBuilder().setHighFreqState(body).build()).getHighFreqState();
        assertEquals(1.5f, parsed.getPosition().getX());
        assertEquals(-2.25f, parsed.getPosition().getY());
        assertEquals(4.2f, parsed.getVelocity().getY());
        assertEquals("Jump", parsed.getAnimState());
        assertFalse(parsed.getFacing());
        assertEquals(87, parsed.getHp());
        assertEquals(66.5f, parsed.getShelterEnergy());
    }

    /** C2S_FragmentCaught：碎片坐标（同接几何判定依据）. */
    @Test
    void c2sFragmentCaughtRoundTrip() throws Exception {
        Envelope env = Envelope.newBuilder().setFragmentCaught(
                C2S_FragmentCaught.newBuilder().setFragmentId(42).setPosX(3.5f).setPosY(-1.2f)).build();
        C2S_FragmentCaught parsed = rt(env).getFragmentCaught();
        assertEquals(42, parsed.getFragmentId());
        assertEquals(3.5f, parsed.getPosX());
        assertEquals(-1.2f, parsed.getPosY());
    }

    /** S2C_Resp：reqId 回显 + code + 文案（R5 回执闭环的 proto 承载）. */
    @Test
    void s2cRespRoundTrip() throws Exception {
        Envelope env = Envelope.newBuilder().setResp(S2C_Resp.newBuilder()
                .setReqId(9).setCode(3002).setMessage("人数未满，无法开始")).build();
        S2C_Resp parsed = rt(env).getResp();
        assertEquals(9, parsed.getReqId());
        assertEquals(3002, parsed.getCode());
        assertEquals("人数未满，无法开始", parsed.getMessage());
    }

    /** S2C_ConnectAck / GameStart / PlayerJoined. */
    @Test
    void s2cRoomLifecycleRoundTrip() throws Exception {
        S2C_ConnectAck ack = rt(Envelope.newBuilder().setConnectAck(
                S2C_ConnectAck.newBuilder().setPlayerId(1).setRoomCode("AB12")).build()).getConnectAck();
        assertEquals(1, ack.getPlayerId());
        assertEquals("AB12", ack.getRoomCode());

        S2C_GameStart start = rt(Envelope.newBuilder().setGameStart(
                S2C_GameStart.newBuilder().setChapter(2).setSection(3).setRound(1)).build()).getGameStart();
        assertEquals(2, start.getChapter());
        assertEquals(3, start.getSection());
        assertEquals(1, start.getRound());

        S2C_PlayerJoined joined = rt(Envelope.newBuilder().setPlayerJoined(
                S2C_PlayerJoined.newBuilder().setPlayerId(1).setPlayerCount(2)).build()).getPlayerJoined();
        assertEquals(2, joined.getPlayerCount());
    }

    /** S2C_PhaseChange：阶段枚举 + 时钟差值法三件套. */
    @Test
    void s2cPhaseChangeRoundTrip() throws Exception {
        Envelope env = Envelope.newBuilder()
                .setTimestamp(1000L)
                .setPlayerId(-1)
                .setPhaseChange(S2C_PhaseChange.newBuilder()
                        .setPhase(GamePhasePb.DISASTER_IMPACT)
                        .setDurationMs(20000)
                        .setPhaseEndTime(21000L))
                .build();
        Envelope parsed = rt(env);
        S2C_PhaseChange pc = parsed.getPhaseChange();
        assertEquals(GamePhasePb.DISASTER_IMPACT, pc.getPhase());
        assertEquals(20000, pc.getDurationMs());
        assertEquals(21000L, pc.getPhaseEndTime());
        // 剩余时间 = phase_end_time - 信封 timestamp（时钟差值法）
        assertEquals(20000L, pc.getPhaseEndTime() - parsed.getTimestamp());
        // 0=UNSPECIFIED + 7 阶段（Java 枚举另有 UNRECOGNIZED 哨兵，不计入语义值）
        assertEquals(GamePhasePb.GAME_PHASE_UNSPECIFIED, GamePhasePb.forNumber(0));
        assertEquals(GamePhasePb.PREVIEW, GamePhasePb.forNumber(1));
        assertEquals(GamePhasePb.FRAGMENT_COLLECT, GamePhasePb.forNumber(2));
        assertEquals(GamePhasePb.DISASTER_PREVIEW, GamePhasePb.forNumber(3));
        assertEquals(GamePhasePb.BUILD, GamePhasePb.forNumber(4));
        assertEquals(GamePhasePb.DISASTER_IMPACT, GamePhasePb.forNumber(5));
        assertEquals(GamePhasePb.REST, GamePhasePb.forNumber(6));
        assertEquals(GamePhasePb.UPGRADE, GamePhasePb.forNumber(7));
    }

    /** S2C_MidFreqState：嵌套 PlayerMidFreq + repeated 背包（shelter_energy 为 float）. */
    @Test
    void s2cMidFreqRoundTrip() throws Exception {
        S2C_MidFreqState body = S2C_MidFreqState.newBuilder()
                .addPlayers(S2C_MidFreqState.PlayerMidFreq.newBuilder()
                        .setPlayerId(0).setHp(95).setShelterEnergy(33.7f)
                        .addCarriedFragments(0).addCarriedFragments(1).addCarriedFragments(1))
                .addPlayers(S2C_MidFreqState.PlayerMidFreq.newBuilder()
                        .setPlayerId(1).setHp(100).setShelterEnergy(100f))
                .build();
        S2C_MidFreqState parsed = rt(Envelope.newBuilder().setMidFreqState(body).build()).getMidFreqState();
        assertEquals(2, parsed.getPlayersCount());
        assertEquals(33.7f, parsed.getPlayers(0).getShelterEnergy());
        assertArrayEquals(new int[]{0, 1, 1}, parsed.getPlayers(0).getCarriedFragmentsList().stream().mapToInt(Integer::intValue).toArray());
        assertEquals(0, parsed.getPlayers(1).getCarriedFragmentsCount());
    }

    /** S2C_FragmentDropPlan：PlanItem 嵌套 + int64 seed. */
    @Test
    void s2cFragmentDropPlanRoundTrip() throws Exception {
        S2C_FragmentDropPlan body = S2C_FragmentDropPlan.newBuilder()
                .addPlan(S2C_FragmentDropPlan.PlanItem.newBuilder()
                        .setFragmentId(101).setType(2)
                        .setPosition(Vec2.newBuilder().setX(-9.9f).setY(12.3f))
                        .setDropTime(5.5f).setSeed(9876543210L))
                .build();
        S2C_FragmentDropPlan.PlanItem item = rt(Envelope.newBuilder().setFragmentDropPlan(body).build())
                .getFragmentDropPlan().getPlan(0);
        assertEquals(101, item.getFragmentId());
        assertEquals(2, item.getType());
        assertEquals(-9.9f, item.getPosition().getX());
        assertEquals(5.5f, item.getDropTime());
        assertEquals(9876543210L, item.getSeed());
    }

    /** S2C_FragmentResult：同接语义四件套. */
    @Test
    void s2cFragmentResultRoundTrip() throws Exception {
        S2C_FragmentResult parsed = rt(Envelope.newBuilder().setFragmentResult(S2C_FragmentResult.newBuilder()
                .setFragmentId(101).setPlayerId(0).setMultiplier(2).setIsSimultaneous(true)).build())
                .getFragmentResult();
        assertEquals(2, parsed.getMultiplier());
        assertTrue(parsed.getIsSimultaneous());
    }

    /** S2C_OpponentDisconnect：state + 离开者走信封 player_id. */
    @Test
    void s2cOpponentDisconnectRoundTrip() throws Exception {
        Envelope parsed = rt(Envelope.newBuilder()
                .setPlayerId(1)
                .setOpponentDisconnect(S2C_OpponentDisconnect.newBuilder().setState("lobby"))
                .build());
        assertEquals("lobby", parsed.getOpponentDisconnect().getState());
        assertEquals(1, parsed.getPlayerId());
    }

    /** S2C_HeartbeatAck：serverTimestamp（应用层 RTT 基准）. */
    @Test
    void s2cHeartbeatAckRoundTrip() throws Exception {
        S2C_HeartbeatAck parsed = rt(Envelope.newBuilder().setHeartbeatAck(
                S2C_HeartbeatAck.newBuilder().setServerTimestamp(1691908800900L)).build()).getHeartbeatAck();
        assertEquals(1691908800900L, parsed.getServerTimestamp());
    }

    /** 坏字节：parseFrom 抛异常（PS-1 ProtoCodec 兜底为 null → 1002 回执）. */
    @Test
    void malformedBytesThrow() {
        assertThrows(InvalidProtocolBufferException.class, () -> Envelope.parseFrom(new byte[]{(byte) 0xFF, 0x00, 0x01}));
    }

    /** 空 body（BODY_NOT_SET）：合法编码但无路由键，服务器按 1002 处理. */
    @Test
    void emptyEnvelopeParsesWithBodyNotSet() throws Exception {
        Envelope parsed = rt(Envelope.newBuilder().setTimestamp(1L).build());
        assertEquals(Envelope.BodyCase.BODY_NOT_SET, parsed.getBodyCase());
    }
}
