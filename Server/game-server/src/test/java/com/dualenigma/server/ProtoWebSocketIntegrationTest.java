package com.dualenigma.server;

import com.dualenigma.v1.C2S_Connect;
import com.dualenigma.v1.C2S_FragmentCaught;
import com.dualenigma.v1.C2S_HighFreqState;
import com.dualenigma.v1.Envelope;
import com.dualenigma.v1.S2C_FragmentDropPlan;
import com.dualenigma.v1.Vec2;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Timeout;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.web.socket.BinaryMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.client.WebSocketClient;
import org.springframework.web.socket.client.standard.StandardWebSocketClient;
import org.springframework.web.socket.handler.BinaryWebSocketHandler;

import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * PS-1 联调验收（服务器侧）：双客户端真实 WebSocket + proto 二进制帧全链路.
 * 覆盖：R5 回执矩阵（0/3001/3002/1002）、Resp 先于 ConnectAck、进房广播、
 * 开局广播、PhaseChange、掉落计划、高频位置入库 + 几何同接仲裁、心跳 Ack.
 */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class ProtoWebSocketIntegrationTest {

    @LocalServerPort
    int port;

    private WebSocketSession sessionA;
    private WebSocketSession sessionB;

    /** 收集全部二进制帧；awaitBody 非破坏性（未匹配消息进缓冲区，不丢弃） */
    static class Collector extends BinaryWebSocketHandler {
        private final BlockingQueue<Envelope> received = new LinkedBlockingQueue<>();
        private final List<Envelope> pending = new ArrayList<>();

        @Override
        protected void handleBinaryMessage(WebSocketSession session, BinaryMessage message) {
            ByteBuffer buf = message.getPayload();
            byte[] bytes = new byte[buf.remaining()];
            buf.get(bytes);
            try {
                received.add(Envelope.parseFrom(bytes));
            } catch (Exception ignored) {
            }
        }

        /** 等待指定 body 类型（未匹配的消息留在缓冲区供后续断言使用） */
        synchronized Envelope awaitBody(Envelope.BodyCase bodyCase, long timeoutMs) throws InterruptedException {
            long deadline = System.currentTimeMillis() + timeoutMs;
            while (System.currentTimeMillis() < deadline) {
                for (Iterator<Envelope> it = pending.iterator(); it.hasNext(); ) {
                    Envelope env = it.next();
                    if (env.getBodyCase() == bodyCase) {
                        it.remove();
                        return env;
                    }
                }
                Envelope env = received.poll(100, TimeUnit.MILLISECONDS);
                if (env != null) {
                    if (env.getBodyCase() == bodyCase) {
                        return env;
                    }
                    pending.add(env);
                }
            }
            return null;
        }

        /** 顺序断言：取队头（用于验证 Resp → ConnectAck 的先后） */
        synchronized Envelope awaitAny(long timeoutMs) throws InterruptedException {
            if (!pending.isEmpty()) {
                return pending.remove(0);
            }
            return received.poll(timeoutMs, TimeUnit.MILLISECONDS);
        }
    }

    @AfterEach
    void tearDown() throws Exception {
        if (sessionA != null && sessionA.isOpen()) sessionA.close();
        if (sessionB != null && sessionB.isOpen()) sessionB.close();
    }

    private void send(WebSocketSession session, Envelope env) throws Exception {
        session.sendMessage(new BinaryMessage(env.toByteArray()));
    }

    private static void assertResp(Envelope env, int reqId, int code) {
        assertNotNull(env, "resp envelope must arrive");
        assertEquals(Envelope.BodyCase.RESP, env.getBodyCase(), "expect resp, got " + (env == null ? "null" : env.getBodyCase()));
        assertEquals(reqId, env.getResp().getReqId(), "reqId echo");
        assertEquals(code, env.getResp().getCode(), "resp code");
    }

    @Test
    @Timeout(60)
    void fullLobbyAndGameFlowOverProtobufBinary() throws Exception {
        WebSocketClient client = new StandardWebSocketClient();
        String uri = "ws://localhost:" + port + "/game";

        Collector collectorA = new Collector();
        Collector collectorB = new Collector();
        sessionA = client.execute(collectorA, uri).get(5, TimeUnit.SECONDS);
        sessionB = client.execute(collectorB, uri).get(5, TimeUnit.SECONDS);

        // ── 1. A 自动匹配建房：Resp(0) 必须先于 ConnectAck 到达 ──
        send(sessionA, Envelope.newBuilder().setReqId(1)
                .setConnect(C2S_Connect.newBuilder()).build());
        Envelope first = collectorA.awaitAny(5000);
        Envelope second = collectorA.awaitAny(5000);
        assertNotNull(first);
        assertNotNull(second);
        assertResp(first, 1, 0);
        assertEquals(Envelope.BodyCase.CONNECT_ACK, second.getBodyCase());
        assertEquals(0, second.getConnectAck().getPlayerId(), "host = playerId 0");
        String roomCode = second.getConnectAck().getRoomCode();
        assertTrue(!roomCode.isEmpty(), "room code assigned");

        // 建房即自加入广播：PlayerJoined(count=1, playerId=0)
        Envelope selfJoin = collectorA.awaitBody(Envelope.BodyCase.PLAYER_JOINED, 5000);
        assertNotNull(selfJoin, "self PlayerJoined on room create");
        assertEquals(0, selfJoin.getPlayerJoined().getPlayerId());
        assertEquals(1, selfJoin.getPlayerJoined().getPlayerCount());

        // ── 2. A 单人开局 → 3002 未满员（R5 拒绝回执）──
        send(sessionA, Envelope.newBuilder().setReqId(2)
                .setStartGame(com.dualenigma.v1.C2S_StartGame.getDefaultInstance()).build());
        assertResp(collectorA.awaitBody(Envelope.BodyCase.RESP, 5000), 2, 3002);

        // ── 3. B 带房间码进房：Resp(0) + ConnectAck(1)；A 收 PlayerJoined(满员) ──
        send(sessionB, Envelope.newBuilder().setReqId(3)
                .setConnect(C2S_Connect.newBuilder().setRoomCode(roomCode)).build());
        assertResp(collectorB.awaitBody(Envelope.BodyCase.RESP, 5000), 3, 0);
        Envelope ackB = collectorB.awaitBody(Envelope.BodyCase.CONNECT_ACK, 5000);
        assertNotNull(ackB);
        assertEquals(1, ackB.getConnectAck().getPlayerId(), "joiner = playerId 1");
        Envelope joined = collectorA.awaitBody(Envelope.BodyCase.PLAYER_JOINED, 5000);
        assertNotNull(joined);
        assertEquals(2, joined.getPlayerJoined().getPlayerCount(), "room full");

        // ── 4. B 非房主开局 → 3001（R5 拒绝回执）──
        send(sessionB, Envelope.newBuilder().setReqId(4)
                .setStartGame(com.dualenigma.v1.C2S_StartGame.getDefaultInstance()).build());
        assertResp(collectorB.awaitBody(Envelope.BodyCase.RESP, 5000), 4, 3001);

        // ── 5. 房主开局：Resp(7,0) + 双方 GameStart + PhaseChange(Preview) + 掉落计划 ──
        send(sessionA, Envelope.newBuilder().setReqId(7)
                .setStartGame(com.dualenigma.v1.C2S_StartGame.getDefaultInstance()).build());
        assertResp(collectorA.awaitBody(Envelope.BodyCase.RESP, 5000), 7, 0);
        assertNotNull(collectorA.awaitBody(Envelope.BodyCase.GAME_START, 5000), "A GameStart");
        assertNotNull(collectorB.awaitBody(Envelope.BodyCase.GAME_START, 5000), "B GameStart");

        Envelope phaseA = collectorA.awaitBody(Envelope.BodyCase.PHASE_CHANGE, 5000);
        assertNotNull(phaseA, "A PhaseChange");
        assertEquals(com.dualenigma.v1.GamePhasePb.PREVIEW, phaseA.getPhaseChange().getPhase());
        assertEquals(5000, phaseA.getPhaseChange().getDurationMs());
        // 时钟差值法：phase_end_time 晚于信封 timestamp
        assertTrue(phaseA.getPhaseChange().getPhaseEndTime() > phaseA.getTimestamp());

        Envelope dropA = collectorA.awaitBody(Envelope.BodyCase.FRAGMENT_DROP_PLAN, 5000);
        assertNotNull(dropA, "A FragmentDropPlan");
        S2C_FragmentDropPlan.PlanItem item0 = dropA.getFragmentDropPlan().getPlan(0);
        assertTrue(dropA.getFragmentDropPlan().getPlanCount() >= 30, "5 preview + 25 collect items");
        assertNotNull(collectorB.awaitBody(Envelope.BodyCase.FRAGMENT_DROP_PLAN, 5000), "B FragmentDropPlan");

        // ── 6. 高频位置入库（A 站到碎片 0 正下方）→ 几何仲裁单接 ──
        send(sessionA, Envelope.newBuilder()
                .setHighFreqState(C2S_HighFreqState.newBuilder()
                        .setPosition(Vec2.newBuilder().setX(item0.getPosition().getX()).setY(item0.getPosition().getY()))
                        .setVelocity(Vec2.newBuilder().setX(0f).setY(0f))
                        .setAnimState("Idle").setFacing(true).setHp(100).setShelterEnergy(100f))
                .build());
        Thread.sleep(300);   // 等服务器入库权威快照

        send(sessionA, Envelope.newBuilder().setReqId(8)
                .setFragmentCaught(C2S_FragmentCaught.newBuilder()
                        .setFragmentId(item0.getFragmentId())
                        .setPosX(item0.getPosition().getX())
                        .setPosY(item0.getPosition().getY()))
                .build());
        assertResp(collectorA.awaitBody(Envelope.BodyCase.RESP, 5000), 8, 0);
        Envelope result = collectorB.awaitBody(Envelope.BodyCase.FRAGMENT_RESULT, 5000);
        assertNotNull(result, "B FragmentResult");
        assertEquals(item0.getFragmentId(), result.getFragmentResult().getFragmentId());
        assertEquals(1, result.getFragmentResult().getMultiplier(), "solo catch = x1");
        assertEquals(false, result.getFragmentResult().getIsSimultaneous());

        // ── 7. 心跳豁免路径：heartbeat → HeartbeatAck ──
        send(sessionA, Envelope.newBuilder()
                .setHeartbeat(com.dualenigma.v1.C2S_Heartbeat.getDefaultInstance()).build());
        Envelope ack = collectorA.awaitBody(Envelope.BodyCase.HEARTBEAT_ACK, 5000);
        assertNotNull(ack, "HeartbeatAck");
        assertTrue(ack.getHeartbeatAck().getServerTimestamp() > 0);

        // ── 8. 坏字节 → 1002 兜底（reqId 拿不到回 0）──
        sessionA.sendMessage(new BinaryMessage(new byte[]{(byte) 0xFF, 0x00, 0x01, 0x02}));
        assertResp(collectorA.awaitBody(Envelope.BodyCase.RESP, 5000), 0, 1002);
    }
}
