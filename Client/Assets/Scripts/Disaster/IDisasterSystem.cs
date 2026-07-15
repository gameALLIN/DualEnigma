/// ============================================================
/// 文件名: IDisasterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难系统服务接口。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难系统服务接口，注册到 ServiceLocator。
    /// 引用：灾难系统.md §3.2
    /// </summary>
    public interface IDisasterSystem
    {
        /// <summary>当前运行的灾难</summary>
        DisasterBase CurrentDisaster { get; }

        /// <summary>启动灾难</summary>
        void StartDisaster(DisasterId disasterId, float difficultyMultiplier, uint seed);

        /// <summary>停止灾难</summary>
        void StopDisaster();

        /// <summary>每帧更新</summary>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// 获取当前灾难的实际位置（世界坐标）。
        /// 若无运行中的灾难，返回 Vector2.zero。
        /// </summary>
        Vector2 GetDisasterPosition();
    }
}
