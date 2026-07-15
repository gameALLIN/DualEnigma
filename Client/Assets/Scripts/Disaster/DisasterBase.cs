/// ============================================================
/// 文件名: DisasterBase.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难基类，所有35种灾难继承此类。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难基类。所有35种灾难继承此类。
    /// 生命周期：OnStart → OnUpdate(渐进强度) → OnEnd
    /// 引用：灾难系统.md §3.1
    /// </summary>
    public abstract class DisasterBase
    {
        /// <summary>灾难参数</summary>
        public DisasterParams Params { get; protected set; }

        /// <summary>当前强度（0-1）</summary>
        public float CurrentIntensity { get; protected set; }

        /// <summary>是否正在运行</summary>
        public bool IsRunning { get; protected set; }

        /// <summary>已运行时间</summary>
        protected float ElapsedTime;

        /// <summary>灾难开始</summary>
        public abstract void OnStart();

        /// <summary>每帧更新</summary>
        public abstract void OnUpdate(float deltaTime, float elapsedTime);

        /// <summary>灾难结束</summary>
        public abstract void OnEnd();

        /// <summary>
        /// 计算当前渐进强度。
        /// 引用：灾难系统设计.md §6.3 渐进入侵节奏
        /// </summary>
        protected virtual float CalculateIntensity(float elapsedTime)
        {
            if (elapsedTime < 5f)
                return Mathf.Lerp(0f, 0.3f, elapsedTime / 5f);
            if (elapsedTime < 10f)
                return Mathf.Lerp(0.3f, 0.6f, (elapsedTime - 5f) / 5f);
            if (elapsedTime < 15f)
                return Mathf.Lerp(0.6f, 1.0f, (elapsedTime - 10f) / 5f);
            return Mathf.Lerp(1.0f, 0.8f, (elapsedTime - 15f) / 5f);
        }

        /// <summary>初始化灾难参数</summary>
        public virtual void Initialize(DisasterParams parameters)
        {
            Params = parameters;
            CurrentIntensity = 0f;
            IsRunning = false;
            ElapsedTime = 0f;
        }
    }
}
