/// ============================================================
/// 文件名: ClickEffectSystem.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 点击反馈系统（常驻单例）：全局点击捕获 + 特效/音效统一入口。
///       自动层：游戏画面点击 → RingPulse（z=0 世界平面）；
///               UI 点击 → StarTwinkle（Effect 层覆盖相机渲染在 UI 之上）。
///       显式层：关键按钮 onClick 调 Play(type, transform) 播强化反馈
///               （开始对局=Shockwave，邀请=ElementMix）。
///       预制体来源：ArtResources/Prefabs/Effects/Click/ClickEffect_{type}
///       （由菜单 DualEnigma/生成点击特效预制体 生成，播完自动销毁）。
///       音效：ClickSfxGenerator 按类型程序化合成，PlayOneShot 播放。
/// 引用：ClickEffectEnums.cs, ClickSfxGenerator.cs, GameLaunch.cs
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DualEnigma.Framework.Core;

namespace DualEnigma.Art
{
    /// <summary>点击反馈系统：特效 + 音效统一入口</summary>
    public class ClickEffectSystem : Singleton<ClickEffectSystem>
    {
        /// <summary>UI 层特效渲染序（高于全部 UI 默认层）</summary>
        private const int UI_SORTING_ORDER = 500;

        /// <summary>世界层特效渲染序（高于建筑 2，低于 UI）</summary>
        private const int WORLD_SORTING_ORDER = 5;

        /// <summary>Effect 层（TagManager index 8，UI 之上特效专用渲染层）</summary>
        private const int EFFECT_LAYER = 8;

        private readonly Dictionary<ClickEffectType, GameObject> _prefabs = new Dictionary<ClickEffectType, GameObject>();
        private readonly Dictionary<ClickEffectType, AudioClip> _clips = new Dictionary<ClickEffectType, AudioClip>();

        private AudioSource _audio;
        private Camera _camera;
        private Camera _overlayCamera;
        private bool _prefabWarned;

        protected override void OnSingletonInitialized()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f; // 2D UI 音
            Debug.Log("[ClickEffectSystem] 点击反馈系统初始化完成");
        }

        // ============================================================
        //  全局点击捕获（自动层）
        // ============================================================

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 自动层：世界点击=圆环脉冲 / UI 点击=星光（UI 之上渲染）
            if (overUI)
                Play(ClickEffectType.StarTwinkle, Input.mousePosition, aboveUI: true, withSound: false);
            else
                Play(ClickEffectType.RingPulse, Input.mousePosition, aboveUI: false, withSound: true);
        }

        // ============================================================
        //  统一播放入口（显式层：按钮 onClick / 游戏逻辑调用）
        // ============================================================

        /// <summary>在 UI 元素位置播放强化反馈（开始对局=Shockwave / 邀请=ElementMix 等）</summary>
        public static void Play(ClickEffectType type, Component uiElement, bool withSound = true)
        {
            if (uiElement == null) return;
            Camera cam = Instance._camera != null ? Instance._camera : Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(uiElement.transform.position);
            Play(type, screen, aboveUI: true, withSound);
        }

        /// <summary>在屏幕坐标播放（aboveUI=true 时特效渲染在 UI 之上）</summary>
        public static void Play(ClickEffectType type, Vector3 screenPoint, bool aboveUI = false, bool withSound = true)
        {
            if (!HasInstance) return;
            Instance.PlayInternal(type, screenPoint, aboveUI, withSound);
        }

        private void PlayInternal(ClickEffectType type, Vector3 screenPoint, bool aboveUI, bool withSound)
        {
            Camera cam = _camera != null ? _camera : (_camera = Camera.main);
            if (cam == null) return;

            // 特效：屏幕点投影到 z=0 世界平面
            GameObject prefab = GetPrefab(type);
            if (prefab != null)
            {
                // UI 之上渲染：UI 画布由独立 UI Camera（depth 0）后画，世界粒子
                // 的 sortingOrder 跨相机无效，必须换 Effect 层 + 更高 depth 的覆盖相机
                Camera renderCam = cam;
                if (aboveUI)
                {
                    renderCam = GetOverlayCamera(cam) ?? cam;
                }

                Vector3 world = renderCam.ScreenToWorldPoint(new Vector3(
                    screenPoint.x, screenPoint.y, -renderCam.transform.position.z));

                GameObject instance = Instantiate(prefab, world, Quaternion.identity);

                if (aboveUI)
                    SetLayerRecursive(instance.transform, EFFECT_LAYER);

                // 渲染层级：UI 反馈由覆盖相机画在 UI 之上；世界反馈高于建筑
                int order = aboveUI ? UI_SORTING_ORDER : WORLD_SORTING_ORDER;
                foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.randomSeed = (uint)Random.Range(1, int.MaxValue); // 每次形态不同
                    ps.Play();

                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                        renderer.sortingOrder = order;
                }
            }

            // 音效：程序化合成，按主题变调
            if (withSound && _audio != null)
            {
                AudioClip clip = GetClip(type);
                if (clip != null)
                    _audio.PlayOneShot(clip);
            }
        }

        // ============================================================
        //  UI 覆盖相机（Effect 层专用，depth 高于 UI Camera）
        // ============================================================

        /// <summary>
        /// 懒创建 UI 覆盖相机：与主相机同变换/同正交尺寸（屏幕↔世界映射、粒子
        /// 缩放与世界特效一致），Depth 清屏，只渲染 Effect 层，最后绘制（depth=1）。
        /// </summary>
        private Camera GetOverlayCamera(Camera mainCam)
        {
            if (_overlayCamera != null)
                return _overlayCamera;

            GameObject go = new GameObject("ClickEffectOverlayCamera");
            go.transform.SetParent(transform, false);
            _overlayCamera = go.AddComponent<Camera>();
            _overlayCamera.orthographic = true;
            _overlayCamera.orthographicSize = mainCam.orthographicSize;
            go.transform.position = mainCam.transform.position;
            _overlayCamera.clearFlags = CameraClearFlags.Depth;
            _overlayCamera.cullingMask = 1 << EFFECT_LAYER;
            _overlayCamera.depth = 1; // 高于 UI Camera(0)，特效渲染在全部 UI 之上

            return _overlayCamera;
        }

        /// <summary>递归设置层级（粒子子系统一并切换到 Effect 层）</summary>
        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i), layer);
        }

        // ============================================================
        //  资源加载（懒加载缓存）
        // ============================================================

        private GameObject GetPrefab(ClickEffectType type)
        {
            if (_prefabs.TryGetValue(type, out GameObject prefab) && prefab != null)
                return prefab;

#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/ArtResources/Prefabs/Effects/Click/ClickEffect_{type}.prefab");
#else
            // TODO(AB 接入): ArtResources/Prefabs/Effects/Click 纳入 effect bundle 后改走 ResMgr
            prefab = Framework.Core.ResMgr.Instance.LoadPrefab($"Prefabs/Effects/Click/ClickEffect_{type}");
#endif
            if (prefab == null)
            {
                if (!_prefabWarned)
                {
                    _prefabWarned = true;
                    Debug.LogWarning($"[ClickEffectSystem] 点击特效预制体缺失（首个缺失类型: {type}）。" +
                        "请先运行菜单 DualEnigma/生成点击特效预制体");
                }
                return null;
            }

            _prefabs[type] = prefab;
            return prefab;
        }

        private AudioClip GetClip(ClickEffectType type)
        {
            if (_clips.TryGetValue(type, out AudioClip clip) && clip != null)
                return clip;

            clip = ClickSfxGenerator.GenerateClip(type);
            _clips[type] = clip;
            return clip;
        }
    }
}
