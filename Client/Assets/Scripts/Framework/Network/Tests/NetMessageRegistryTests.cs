/// ============================================================
/// 文件名: NetMessageRegistryTests.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: NetMessageRegistry 单元测试 — 注册/派发/未知类型忽略/
///       handler 异常隔离/Unregister 后不派发/信封透传
/// ============================================================

using System;
using NUnit.Framework;
using DualEnigma.Framework.Network;

namespace DualEnigma.Framework.Network.Tests
{
    /// <summary>测试用消息 DTO（结构与线上一致：type + 嵌套 data）</summary>
    [Serializable]
    public class TestMsg : INetMessage
    {
        public string type;
        public Data data;

        [Serializable]
        public class Data
        {
            public int value;
            public string text;
        }
    }

    [TestFixture]
    public class NetMessageRegistryTests
    {
        private NetMessageRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new NetMessageRegistry();
        }

        [Test]
        public void Register_And_Dispatch_InvokesHandler()
        {
            int received = 0;
            _registry.Register<TestMsg>("T_Test", msg => received++);

            _registry.Dispatch("{\"type\":\"T_Test\",\"data\":{\"value\":42,\"text\":\"hi\"}}");

            Assert.AreEqual(1, received, "handler 应被调用一次");
        }

        [Test]
        public void Dispatch_ParsesBodyFields()
        {
            int value = 0;
            _registry.Register<TestMsg>("T_Test", msg => value = msg.data.value);

            _registry.Dispatch("{\"type\":\"T_Test\",\"data\":{\"value\":7,\"text\":\"x\"}}");

            Assert.AreEqual(7, value, "data 字段应正确解析");
        }

        [Test]
        public void Dispatch_UnknownType_IsIgnored()
        {
            int received = 0;
            _registry.Register<TestMsg>("T_Test", msg => received++);

            // 未知类型：静默忽略（不应抛错、不应调用）
            Assert.DoesNotThrow(() => _registry.Dispatch("{\"type\":\"T_Other\",\"data\":{}}"));
            Assert.AreEqual(0, received);
        }

        [Test]
        public void Dispatch_HandlerException_DoesNotBreakStream()
        {
            bool secondCalled = false;
            _registry.Register<TestMsg>("T_Throw", msg => throw new InvalidOperationException("boom"));
            _registry.Register<TestMsg>("T_After", msg => secondCalled = true);

            // 异常 handler 不应向外抛
            Assert.DoesNotThrow(() => _registry.Dispatch("{\"type\":\"T_Throw\",\"data\":{}}"));
            // 后续消息仍可派发（异常隔离）
            _registry.Dispatch("{\"type\":\"T_After\",\"data\":{}}");
            Assert.IsTrue(secondCalled, "异常后注册表应继续工作");
        }

        [Test]
        public void Unregister_PreventsDispatch()
        {
            int received = 0;
            Action<TestMsg> handler = msg => received++;
            _registry.Register("T_Test", handler);
            _registry.Unregister("T_Test", handler);

            _registry.Dispatch("{\"type\":\"T_Test\",\"data\":{}}");

            Assert.AreEqual(0, received, "注销后不应再派发");
            Assert.AreEqual(0, _registry.Count, "注册表应为空");
        }

        [Test]
        public void Dispatch_EnvelopeHandler_ReceivesTimestamp()
        {
            long seenTimestamp = 0;
            _registry.Register<TestMsg>("T_Test", (envelope, body) => seenTimestamp = envelope.timestamp);

            _registry.Dispatch("{\"type\":\"T_Test\",\"timestamp\":1761234567890,\"data\":{}}");

            Assert.AreEqual(1761234567890L, seenTimestamp, "信封 handler 应收到 timestamp");
        }

        [Test]
        public void Dispatch_MalformedJson_IsIgnored()
        {
            int received = 0;
            _registry.Register<TestMsg>("T_Test", msg => received++);

            Assert.DoesNotThrow(() => _registry.Dispatch("not a json"));
            Assert.DoesNotThrow(() => _registry.Dispatch(""));
            Assert.AreEqual(0, received);
        }

        [Test]
        public void Register_Overwrite_SameType_ReplacesHandler()
        {
            int first = 0, second = 0;
            _registry.Register<TestMsg>("T_Test", msg => first++);
            _registry.Register<TestMsg>("T_Test", msg => second++);

            _registry.Dispatch("{\"type\":\"T_Test\",\"data\":{}}");

            Assert.AreEqual(0, first, "同 type 重复注册应覆盖旧 handler");
            Assert.AreEqual(1, second);
        }
    }
}
