/// ============================================================
/// 文件名: ProtoRoundTripTests.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: Protobuf 协议 round-trip 单测（PC-1 Task C1.6，与服务器篇
///       ProtoRoundTripTest 镜像）：16 种在用消息构造 → ToByteArray →
///       ParseFrom → 字段断言；重点覆盖信封字段（reqId/timestamp/playerId）
///       与 oneof 互斥；附 C2S_HighFreqState 体积断言（<100B，带宽收益锚点）。
/// 引用：Generated/Game.cs（Dualenigma.V1）
/// ============================================================

using NUnit.Framework;
using Google.Protobuf;
using DualEnigma.V1;
using Pb = DualEnigma.V1;

namespace DualEnigma.Network.Tests
{
    [TestFixture]
    public class ProtoRoundTripTests
    {
        private static Envelope RoundTrip(Envelope env)
        {
            return Envelope.Parser.ParseFrom(env.ToByteArray());
        }

        // ── 信封字段 ──

        [Test]
        public void Envelope_ReqId_Timestamp_PlayerId_Survive()
        {
            var env = new Envelope
            {
                ReqId = 7,
                Timestamp = 1761234567890L,
                PlayerId = -1,
                StartGame = new Pb.C2S_StartGame(),
            };

            Envelope back = RoundTrip(env);

            Assert.AreEqual(7, back.ReqId);
            Assert.AreEqual(1761234567890L, back.Timestamp);
            Assert.AreEqual(-1, back.PlayerId);
        }

        [Test]
        public void Envelope_Oneof_IsExclusive()
        {
            var env = new Envelope { StartGame = new Pb.C2S_StartGame() };
            // 后设覆盖先设（oneof 互斥）
            env.Heartbeat = new Pb.C2S_Heartbeat();

            Envelope back = RoundTrip(env);
            Assert.AreEqual(Envelope.BodyOneofCase.Heartbeat, back.BodyCase);
            Assert.AreNotEqual(Envelope.BodyOneofCase.StartGame, back.BodyCase);
        }

        // ── C2S ──

        [Test]
        public void C2S_Connect_RoundTrip()
        {
            var env = new Envelope
            {
                ReqId = 3,
                Connect = new Pb.C2S_Connect { RoomCode = "AB12", Token = "tok-xyz" },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(Envelope.BodyOneofCase.Connect, back.BodyCase);
            Assert.AreEqual("AB12", back.Connect.RoomCode);
            Assert.AreEqual("tok-xyz", back.Connect.Token);
        }

        [Test]
        public void C2S_HighFreqState_RoundTrip_AndSize()
        {
            var env = new Envelope
            {
                HighFreqState = new Pb.C2S_HighFreqState
                {
                    Position = new Pb.Vec2 { X = 1.5f, Y = -2.5f },
                    Velocity = new Pb.Vec2 { X = 0.1f, Y = 4.0f },
                    AnimState = "Run",
                    Facing = true,
                    Hp = 87,
                    ShelterEnergy = 66.5f,
                },
            };

            byte[] bytes = env.ToByteArray();
            Envelope back = Envelope.Parser.ParseFrom(bytes);

            Assert.AreEqual(1.5f, back.HighFreqState.Position.X);
            Assert.AreEqual(-2.5f, back.HighFreqState.Position.Y);
            Assert.AreEqual("Run", back.HighFreqState.AnimState);
            Assert.IsTrue(back.HighFreqState.Facing);
            Assert.AreEqual(87, back.HighFreqState.Hp);
            Assert.AreEqual(66.5f, back.HighFreqState.ShelterEnergy);
            // 带宽断言：满字段高频帧 < 100B（JSON 时代 ~200B）
            Assert.Less(bytes.Length, 100, $"高频帧体积 {bytes.Length}B 应 < 100B");
        }

        [Test]
        public void C2S_FragmentCaught_RoundTrip()
        {
            var env = new Envelope
            {
                ReqId = 11,
                FragmentCaught = new Pb.C2S_FragmentCaught { FragmentId = 42, PosX = 3.2f, PosY = -0.7f },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(42, back.FragmentCaught.FragmentId);
            Assert.AreEqual(3.2f, back.FragmentCaught.PosX);
            Assert.AreEqual(-0.7f, back.FragmentCaught.PosY);
        }

        // ── S2C ──

        [Test]
        public void S2C_Resp_RoundTrip()
        {
            var env = new Envelope
            {
                PlayerId = -1,
                Timestamp = 100L,
                Resp = new Pb.S2C_Resp { ReqId = 7, Code = 2001, Message = "房间不存在或已失效" },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(7, back.Resp.ReqId);
            Assert.AreEqual(2001, back.Resp.Code);
            Assert.AreEqual("房间不存在或已失效", back.Resp.Message);
        }

        [Test]
        public void S2C_ConnectAck_RoundTrip()
        {
            var env = new Envelope
            {
                ConnectAck = new Pb.S2C_ConnectAck { PlayerId = 1, RoomCode = "XY99" },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(1, back.ConnectAck.PlayerId);
            Assert.AreEqual("XY99", back.ConnectAck.RoomCode);
        }

        [Test]
        public void S2C_GameStart_RoundTrip()
        {
            var env = new Envelope { GameStart = new Pb.S2C_GameStart { Chapter = 2, Section = 3, Round = 1 } };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(2, back.GameStart.Chapter);
            Assert.AreEqual(3, back.GameStart.Section);
            Assert.AreEqual(1, back.GameStart.Round);
        }

        [Test]
        public void S2C_PlayerJoined_RoundTrip()
        {
            var env = new Envelope { PlayerJoined = new Pb.S2C_PlayerJoined { PlayerId = 1, PlayerCount = 2 } };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(2, back.PlayerJoined.PlayerCount);
        }

        [Test]
        public void S2C_PhaseChange_RoundTrip()
        {
            var env = new Envelope
            {
                Timestamp = 5000L,
                PhaseChange = new Pb.S2C_PhaseChange
                {
                    Phase = GamePhasePb.Build,
                    DurationMs = 20000,
                    PhaseEndTime = 25000L,
                },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(GamePhasePb.Build, back.PhaseChange.Phase);
            Assert.AreEqual(20000, back.PhaseChange.DurationMs);
            Assert.AreEqual(25000L, back.PhaseChange.PhaseEndTime);
            // 时钟差值法
            Assert.AreEqual(20f, (back.PhaseChange.PhaseEndTime - back.Timestamp) / 1000f);
        }

        [Test]
        public void S2C_MidFreqState_RoundTrip_FloatEnergy()
        {
            var env = new Envelope
            {
                MidFreqState = new Pb.S2C_MidFreqState
                {
                    Players =
                    {
                        new Pb.S2C_MidFreqState.Types.PlayerMidFreq
                        {
                            PlayerId = 0, Hp = 90, ShelterEnergy = 55.7f, CarriedFragments = { 1, 2, 3 },
                        },
                    },
                },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(1, back.MidFreqState.Players.Count);
            Assert.AreEqual(55.7f, back.MidFreqState.Players[0].ShelterEnergy);
            Assert.AreEqual(new[] { 1, 2, 3 }, back.MidFreqState.Players[0].CarriedFragments);
        }

        [Test]
        public void S2C_OpponentDisconnect_PlayerIdOnEnvelope()
        {
            var env = new Envelope
            {
                PlayerId = 1,
                OpponentDisconnect = new Pb.S2C_OpponentDisconnect { State = "lobby" },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(1, back.PlayerId, "离开者 ID 在信封层");
            Assert.AreEqual("lobby", back.OpponentDisconnect.State);
        }

        [Test]
        public void S2C_FragmentDropPlan_RoundTrip()
        {
            var env = new Envelope
            {
                FragmentDropPlan = new Pb.S2C_FragmentDropPlan
                {
                    Plan =
                    {
                        new Pb.S2C_FragmentDropPlan.Types.PlanItem
                        {
                            FragmentId = 9, Type = 2,
                            Position = new Pb.Vec2 { X = -3f, Y = 1f },
                            DropTime = 4.5f, Seed = 123456789L,
                        },
                    },
                },
            };

            Envelope back = RoundTrip(env);
            var item = back.FragmentDropPlan.Plan[0];
            Assert.AreEqual(9, item.FragmentId);
            Assert.AreEqual(2, item.Type);
            Assert.AreEqual(-3f, item.Position.X);
            Assert.AreEqual(123456789L, item.Seed);
        }

        [Test]
        public void S2C_FragmentResult_RoundTrip()
        {
            var env = new Envelope
            {
                FragmentResult = new Pb.S2C_FragmentResult
                {
                    FragmentId = 5, PlayerId = 0, Multiplier = 3, IsSimultaneous = true,
                },
            };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(3, back.FragmentResult.Multiplier);
            Assert.IsTrue(back.FragmentResult.IsSimultaneous);
        }

        [Test]
        public void HeartbeatAck_BodyCase()
        {
            var env = new Envelope { Timestamp = 999L, HeartbeatAck = new Pb.S2C_HeartbeatAck { ServerTimestamp = 999L } };

            Envelope back = RoundTrip(env);
            Assert.AreEqual(Envelope.BodyOneofCase.HeartbeatAck, back.BodyCase);
        }

        [Test]
        public void MalformedBytes_ParseThrows_CaughtByCaller()
        {
            byte[] garbage = { 0xFF, 0xFF, 0xFF, 0xFF };
            // 调用方（GameConnection.OnRawMessage）以 try/catch 兜底坏帧——此处断言抛出行为
            Assert.Throws<InvalidProtocolBufferException>(() => Envelope.Parser.ParseFrom(garbage));
        }
    }
}
