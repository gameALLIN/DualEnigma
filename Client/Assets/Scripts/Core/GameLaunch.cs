/// ============================================================
/// 文件名: GameLaunch.cs
/// 创建时间: 2026-07-10
/// 作者: DualEnigma
/// 描述: 游戏启动入口，负责各系统初始化及首个 UI 面板的加载
/// ============================================================

using DualEnigma.UI;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Data;
using DualEnigma.Character;
using DualEnigma.Fragment;
using DualEnigma.Synthesis;
using DualEnigma.Building;
using DualEnigma.Shelter;
using DualEnigma.Disaster;
using DualEnigma.Skill;
using DualEnigma.Talent;
using DualEnigma.Network;
using UnityEngine;

namespace DualEnigma.Core
{
    /// <summary>
    /// 挂载在启动场景的空 GameObject 上，作为整个游戏的入口点。
    /// 当前阶段仅负责 UI 系统初始化，后续模块按需在此扩展。
    /// </summary>
    public class GameLaunch : MonoBehaviour
    {
        [Header("启动配置")]
        [Tooltip("游戏启动后自动打开的第一个面板名称（需以 UI 开头，对应 Prefabs/UI/{面板名}/{面板名}.prefab）")]
        [SerializeField] private string m_EntryPanelName = "UILogin";

        private void Awake()
        {
            // 初始化资源管理器
            _ = ResMgr.Instance;
            ResMgr.Instance.Init();

            // Runtime 模式下标记常驻 AB
#if !UNITY_EDITOR
            ResMgr.Instance.SetPersistentBundle("ui");
            ResMgr.Instance.SetPersistentBundle("audio");
            ResMgr.Instance.SetPersistentBundle("atlas");
            ResMgr.Instance.SetPersistentBundle("data");
#endif

            // 初始化事件总线
            _ = EventBus.Instance;

            // 初始化数据管理器
            DataManager.Instance.Initialize();

            // 初始化游戏状态机
            _ = GameStateMachine.Instance;

            // 初始化游戏管理器
            _ = GameManager.Instance;

            // 初始化 UI 系统
            _ = UIManager.Instance;

            // 加载地图（背景、天空、地面、墙壁、安全区、网格）
            MapLoader.Load();

            // 确保场景中有正交相机（不再给相机挂 AudioListener，统一在下方收口去重）
            EnsureCamera();

            // AudioListener 统一收口：相机创建完成后再检查，确保全场景有且只有一个
            EnsureSingleAudioListener();

            // 初始化业务系统（触发 Singleton 创建 + ServiceLocator 注册）
            _ = CharacterSystem.Instance;
            CharacterSystem.Instance.Initialize();
            _ = FragmentSystem.Instance;
            _ = SynthesisSystem.Instance;
            _ = BuildingSystem.Instance;
            _ = ShelterSystem.Instance;
            _ = DisasterSystem.Instance;
            _ = SkillSystem.Instance;
            _ = TalentSystem.Instance;
            _ = RoomSession.Instance;      // 会话唯一事实来源（R3：替代 NetworkSystem）
            _ = GameConnection.Instance;   // 游戏连接（R3：替代 GameServerClient）
            _ = AuthService.Instance;

            // 对局流程编排器（阶段驱动：蓝图/灾害/胜负/进度）
            _ = GameplayDriver.Instance;

            // 点击反馈系统（全局点击特效 + 程序化音效）
            _ = DualEnigma.Art.ClickEffectSystem.Instance;

            // 局内常驻 UI（HUD/设置弹窗/结算面板，仿 UIInvitePopup 常驻模式，默认隐藏）
            UIGameHudCtrl.Ensure();
            UISettingsCtrl.Ensure();
            UIGameOverCtrl.Ensure();

            Debug.Log("[GameLaunch] 全系统初始化完成（Core + 9个业务系统 + 流程编排 + UI）");
        }

        /// <summary>
        /// 确保场景中有且只有一个 AudioListener（挂在 GameLaunch 自身，不随相机动）。
        /// </summary>
        private void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();

            if (listeners.Length > 1)
            {
                // 保留第一个（优先挂在相机上的），销毁其余
                System.Array.Sort(listeners, (a, b) =>
                {
                    bool aOnCamera = a != null && a.GetComponent<Camera>() != null;
                    bool bOnCamera = b != null && b.GetComponent<Camera>() != null;
                    return bOnCamera.CompareTo(aOnCamera);
                });

                for (int i = 1; i < listeners.Length; i++)
                {
                    if (listeners[i] != null)
                        Destroy(listeners[i]);
                }
                Debug.Log($"[GameLaunch] 清理了 {listeners.Length - 1} 个多余的 AudioListener");
            }
            else if (listeners.Length == 0)
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[GameLaunch] 自动添加了 AudioListener");
            }
        }

        /// <summary>
        /// 确保场景中有正交相机，位置覆盖整个地图（40×20格）。
        /// 修复多相机 ClearFlags 冲突：MainCamera 清屏，其他相机仅 Depth。
        /// </summary>
        private void EnsureCamera()
        {
            Camera mainCam = Camera.main;

            if (mainCam == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                Debug.Log("[GameLaunch] 自动创建 MainCamera");
            }

            // Main Camera 负责清屏 + 渲染游戏世界
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.orthographic = true;
            mainCam.orthographicSize = 12f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.backgroundColor = new Color(0.15f, 0.20f, 0.22f, 1f);
            mainCam.depth = -1;

            // 场景中其他相机（如 UI Camera）改为 Depth 模式，不清屏覆盖
            Camera[] allCams = FindObjectsOfType<Camera>();
            foreach (Camera c in allCams)
            {
                if (c != mainCam && c.clearFlags == CameraClearFlags.SolidColor)
                {
                    c.clearFlags = CameraClearFlags.Depth;
                    Debug.Log($"[GameLaunch] 相机 {c.name} ClearFlags 改为 Depth（避免覆盖游戏画面）");
                }
            }
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(m_EntryPanelName))
            {
                OpenEntryPanel(m_EntryPanelName);
            }
        }

        /// <summary>
        /// 通过反射打开指定名称的面板，避免泛型 Push&lt;T&gt; 对类型参数的硬依赖。
        /// 后续如有更复杂的启动流程，可在子类中重写此方法。
        /// </summary>
        protected virtual void OpenEntryPanel(string panelName)
        {
            // 遍历所有程序集查找面板 Controller 类型（{panelName}Ctrl）
            System.Type type = null;
            string typeName = $"{panelName}Ctrl";
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType($"DualEnigma.UI.{typeName}");
                if (type != null)
                    break;
            }

            if (type == null)
            {
                Debug.LogWarning($"[GameLaunch] 未找到面板类型 {typeName}，跳过打开。请先使用「DualEnigma > UI > 生成面板」创建该面板。");
                return;
            }

            // 反射调用 UIManager.Push<T>(UIMode) 泛型方法
            var method = typeof(UIManager).GetMethod("Push", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError("[GameLaunch] UIManager.Push 泛型方法未找到");
                return;
            }

            var genericMethod = method.MakeGenericMethod(type);
            genericMethod.Invoke(UIManager.Instance, new object[] { UIMode.FullScreen });

            Debug.Log($"[GameLaunch] 已打开入口面板: {panelName}");
        }
    }
}
