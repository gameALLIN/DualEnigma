/// ============================================================
/// 文件名: ABRefItem.cs
/// 创建时间: 2026-07-11
/// 作者: DualEnigma
/// 描述: AssetBundle 引用追踪数据结构，包含引用计数、持有者、依赖关系、下载设置等
/// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Core
{
    /// <summary>
    /// 下载与缓存设置标志位
    /// </summary>
    [Flags]
    public enum DownloadSettings
    {
        /// <summary>默认使用 Unity Caching 系统缓存</summary>
        Default = 0,
        /// <summary>不使用缓存，每次重新下载</summary>
        DoNotUseCache = 1,
        /// <summary>强制下载最新版本（忽略本地缓存版本）</summary>
        ForceDownload = 2,
    }

    /// <summary>
    /// AssetBundle 引用追踪项，记录单个 AB 的加载状态、引用计数和持有者信息。
    /// 由 AssetBundleMgr 创建和管理。
    /// </summary>
    public class ABRefItem
    {
        /// <summary>AB 名称（不含扩展名）</summary>
        public string BundleName;

        /// <summary>AB 实例（Mock 测试时为 null）</summary>
        public AssetBundle Bundle;

        /// <summary>当前引用计数</summary>
        public int RefCount;

        /// <summary>持有该 AB 引用的对象列表，用于追踪谁在使用该 AB</summary>
        public List<UnityEngine.Object> Holders = new List<UnityEngine.Object>();

        /// <summary>该 AB 的依赖 AB 列表，由 Manifest.GetAllDependencies 获取</summary>
        public string[] Dependencies;

        /// <summary>下载设置</summary>
        public DownloadSettings Settings;

        /// <summary>是否常驻（不参与卸载）</summary>
        public bool IsPersistent;
    }

    /// <summary>
    /// 延迟卸载队列项，记录 AB 名称和预定卸载时间。
    /// 当 RefCount 归零时加入队列，到达卸载时间后执行 AssetBundle.Unload(true)。
    /// </summary>
    public class DelayUnloadItem
    {
        /// <summary>AB 名称</summary>
        public string BundleName;

        /// <summary>预定卸载时间（Time.time + 延迟秒数）</summary>
        public float UnloadTime;
    }
}