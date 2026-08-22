/// ============================================================
/// 文件名: ClickEffectInput.cs
/// 创建时间: 2026-08-22
/// 最后更新: 2026-08-22
/// 作者: DualEnigma
/// 描述: [演示组件] 点击特效手动播放器（特效选型/调试用）。
///       正式接入请使用 ClickEffectSystem（常驻单例，自动加载预制体 +
///       全局点击捕获 + 分层播放 + 程序化音效），勿再手动拖预制体数组。
///       本组件仅在需要逐个预览 10 种特效时临时挂到场景 GameObject。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>
    /// 点击特效播放组件。
    /// 用法：
    /// 1. 先运行菜单 DualEnigma/生成点击特效预制体（生成 10 个预制体）
    /// 2. 将预制体按 ClickEffectType 枚举顺序（WaterRipple~WarmGlow）拖入 _effectPrefabs
    /// 3. 本组件挂到场景任意 GameObject，运行后点鼠标左键即可看到特效
    /// 播放模式：_playRandom=true 随机轮播（演示/选型），false 固定播放 _effectType。
    /// 引用：ClickEffectEnums.cs, ClickEffectPrefabGenerator.cs
    /// </summary>
    public class ClickEffectInput : MonoBehaviour
    {
        [Header("特效预制体（按 ClickEffectType 枚举顺序拖入，共10个）")]
        [SerializeField] private GameObject[] _effectPrefabs;

        [Header("播放模式")]
        [Tooltip("随机轮播全部特效（演示模式）")]
        [SerializeField] private bool _playRandom = true;
        [Tooltip("固定播放的特效类型（_playRandom=false 时生效）")]
        [SerializeField] private ClickEffectType _effectType = ClickEffectType.RingPulse;

        [Header("相机（空则用 Camera.main）")]
        [SerializeField] private Camera _camera;

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            if (_effectPrefabs == null || _effectPrefabs.Length == 0)
                return;

            GameObject prefab = _playRandom
                ? _effectPrefabs[Random.Range(0, _effectPrefabs.Length)]
                : GetPrefab(_effectType);

            if (prefab == null)
                return;

            PlayAtScreenPoint(prefab, Input.mousePosition, _camera);
        }

        /// <summary>
        /// 按类型取预制体（数组顺序对应 ClickEffectType 枚举）。
        /// </summary>
        private GameObject GetPrefab(ClickEffectType type)
        {
            int index = (int)type;
            return index >= 0 && index < _effectPrefabs.Length ? _effectPrefabs[index] : null;
        }

        /// <summary>
        /// 在屏幕坐标播放特效预制体（自动投影到 z=0 平面）。
        /// 供 UI 按钮/游戏逻辑直接调用。
        /// </summary>
        /// <param name="prefab">特效预制体（生成器产物）</param>
        /// <param name="screenPoint">屏幕坐标（如 Input.mousePosition）</param>
        /// <param name="camera">目标相机（空则用 Camera.main）</param>
        /// <returns>特效实例（播完自动销毁，可无视返回值）</returns>
        public static GameObject PlayAtScreenPoint(
            GameObject prefab, Vector3 screenPoint, Camera camera = null)
        {
            Camera cam = camera != null ? camera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[ClickEffectInput] 未找到相机，无法转换屏幕坐标");
                return null;
            }

            // 2D 正交相机：屏幕点投影到 z=0 平面
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(
                screenPoint.x, screenPoint.y, -cam.transform.position.z));
            return PlayPrefab(prefab, world);
        }

        /// <summary>
        /// 通用播放入口：实例化特效预制体并随机化粒子种子（避免每次点击形态重复）。
        /// 预制体自带 stopAction=Destroy，播完自动销毁，无需手动清理。
        /// </summary>
        /// <param name="prefab">特效预制体（生成器产物）</param>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>特效实例</returns>
        public static GameObject PlayPrefab(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null)
                return null;

            GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity);

            // 重设随机种子：同预制体实例的种子相同，不重设则每次点击形态完全一致
            foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.randomSeed = (uint)Random.Range(1, int.MaxValue);
                ps.Play();
            }

            return instance;
        }
    }
}
