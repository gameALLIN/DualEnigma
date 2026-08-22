/// ============================================================
/// 文件名: ThrottledSenderTests.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: ThrottledSender 单元测试 — 20Hz 放行频率 / 不限频模式 / Reset
/// ============================================================

using NUnit.Framework;
using DualEnigma.Framework.Network;

namespace DualEnigma.Framework.Network.Tests
{
    [TestFixture]
    public class ThrottledSenderTests
    {
        [Test]
        public void Tick_At20Hz_ReleasesAbout20PerSecond()
        {
            var sender = new ThrottledSender(20f);

            // 以 240Hz 帧步进模拟 1 秒（4.17ms/帧）
            int released = 0;
            float step = 1f / 240f;
            for (float t = 0f; t < 1f; t += step)
            {
                if (sender.Tick(step))
                    released++;
            }

            // 到点清零语义：1 秒内放行次数应为 20 左右（允许 ±1 浮点误差）
            Assert.GreaterOrEqual(released, 19, "1 秒至少放行 19 次（20Hz）");
            Assert.LessOrEqual(released, 21, "1 秒至多放行 21 次（20Hz）");
        }

        [Test]
        public void Tick_BelowInterval_ReturnsFalse()
        {
            var sender = new ThrottledSender(20f); // 间隔 0.05s

            Assert.IsFalse(sender.Tick(0.01f), "未到间隔不应放行");
            Assert.IsFalse(sender.Tick(0.01f), "累积 0.02s 仍不应放行");
            Assert.IsFalse(sender.Tick(0.01f), "累积 0.03s 仍不应放行");
            Assert.IsFalse(sender.Tick(0.01f), "累积 0.04s 仍不应放行");
            Assert.IsTrue(sender.Tick(0.01f), "累积 0.05s 到点应放行");
            Assert.IsFalse(sender.Tick(0.01f), "放行后累积清零，不应立即再放行");
        }

        [Test]
        public void Tick_ZeroOrNegativeRate_AlwaysReleases()
        {
            var sender = new ThrottledSender(0f);

            Assert.IsTrue(sender.Tick(0f), "不限频模式每次放行");
            Assert.IsTrue(sender.Tick(0.001f));
        }

        [Test]
        public void Reset_ClearsAccumulator()
        {
            var sender = new ThrottledSender(20f);

            sender.Tick(0.04f);      // 累积 0.04
            sender.Reset();          // 清零
            Assert.IsFalse(sender.Tick(0.01f), "Reset 后累积应清零，0.01s 不放行");

            sender.Tick(0.04f);
            Assert.IsTrue(sender.Tick(0.01f), "累积 0.05s 到点放行");
        }
    }
}
