/// ============================================================
/// 文件名: UISettingsCtrl.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 局内设置弹窗控制器。常驻 UILayer.Top（不进面板栈，
///       仿 UIInvitePopup 模式），由 UIGameHud 的 ESC/设置按钮开关。
///       继续游戏 / 音量滑条（AudioListener.volume + PlayerPrefs）/
///       退出对局（GameManager.ExitToHome）。单机模式开关弹窗伴随暂停。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UISettingsCtrl : UICtrlBase
    {
        /// <summary>预制体路径（相对于 AssetPackage/，与 UIManager 约定一致）</summary>
        private const string PREFAB_PATH = "Prefabs/UI/UISettings/UISettings";

        /// <summary>音量持久化键</summary>
        private const string VOLUME_PREF_KEY = "SettingsVolume";

        /// <summary>默认音量</summary>
        private const float DEFAULT_VOLUME = 0.8f;

        /// <summary>性能信息显示开关持久化键（UIGameHudCtrl 读取同键）</summary>
        private const string PERF_PREF_KEY = "SettingsShowPerf";

        private static UISettingsCtrl s_Instance;

        private UISettingsModel _model;
        private UISettingsView _view;

        /// <summary>当前形态是否允许退出登录（主界面打开=true 显示退出登录；局内打开=false 显示退出对局）</summary>
        private bool _allowLogout;

        /// <summary>弹窗当前是否可见</summary>
        public static bool IsVisible => s_Instance != null && s_Instance.gameObject.activeSelf;

        /// <summary>确保弹窗常驻实例存在（GameLaunch 启动时调用，幂等）</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;

            GameObject prefab = ResMgr.Instance.LoadPrefab(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[UISettings] 预制体加载失败: {PREFAB_PATH}（请先运行菜单 DualEnigma/UI/生成 UISettings 预制体）");
                return;
            }

            RectTransform layerRoot = UIManager.Instance.GetLayerRoot(UILayer.Top);
            if (layerRoot == null)
            {
                Debug.LogError("[UISettings] 未获取到 Top 层级根节点");
                return;
            }

            GameObject popupObj = Instantiate(prefab, layerRoot, false);
            popupObj.name = "UISettings";
            popupObj.transform.SetAsLastSibling();
            popupObj.SetActive(false); // 默认隐藏，由 HUD 开关

            s_Instance = popupObj.GetComponent<UISettingsCtrl>();
            if (s_Instance == null)
            {
                Debug.LogError("[UISettings] 预制体上未找到 UISettingsCtrl 组件");
                Destroy(popupObj);
                return;
            }

            ((IUIPanel)s_Instance).OnCreate();
        }

        /// <summary>打开设置弹窗（单机模式伴随暂停）。allowLogout=true 为主界面形态（显示退出登录，隐藏退出对局）</summary>
        public static void ShowPanel(bool allowLogout = false)
        {
            Ensure();
            if (s_Instance == null) return;
            if (s_Instance.gameObject.activeSelf) return;

            s_Instance._allowLogout = allowLogout;
            s_Instance.gameObject.SetActive(true);
            ((IUIPanel)s_Instance).OnShow();

            // 单机模式：打开设置即暂停；联机模式不可暂停（服务器权威计时）
            if (GameManager.HasInstance && !GameManager.Instance.State.IsGameOver
                && !(NetworkSystem.HasInstance && NetworkSystem.Instance.IsConnected))
            {
                GameManager.Instance.PauseGame();
            }
        }

        /// <summary>关闭设置弹窗（单机模式恢复）</summary>
        public static void HidePanel()
        {
            if (s_Instance == null || !s_Instance.gameObject.activeSelf) return;

            ((IUIPanel)s_Instance).OnHide();
            s_Instance.gameObject.SetActive(false);

            // 单机模式且对局仍在进行 → 恢复（对局结束场景下不恢复）
            if (GameManager.HasInstance && GameManager.Instance.State.IsPaused
                && !GameManager.Instance.State.IsGameOver
                && !(NetworkSystem.HasInstance && NetworkSystem.Instance.IsConnected))
            {
                GameManager.Instance.ResumeGame();
            }
        }

        protected override void OnCreate()
        {
            _model = new UISettingsModel
            {
                // 启动时恢复持久化音量并应用到全局监听器
                Volume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, DEFAULT_VOLUME)
            };
            AudioListener.volume = _model.Volume;

            _view = GetComponent<UISettingsView>();

            // 常驻面板：订阅挂在 OnCreate，销毁时注销
            EventBus.Instance.Subscribe<GameEndEvent>(OnGameEnd);
        }

        protected override void OnShow()
        {
            if (_view == null) return;

            // 双形态：主界面 → 退出登录；局内 → 退出对局（互斥显示）
            if (_view.LogoutBtn != null)
                _view.LogoutBtn.gameObject.SetActive(_allowLogout);
            if (_view.ExitBtn != null)
                _view.ExitBtn.gameObject.SetActive(!_allowLogout);

            // 主界面形态下继续按钮语义为"关闭"
            if (_view.ContinueBtn != null)
            {
                Text continueLabel = _view.ContinueBtn.GetComponentInChildren<Text>();
                if (continueLabel != null)
                    continueLabel.text = _allowLogout ? "关闭" : "继续游戏";
            }

            if (_view.ContinueBtn != null)
                _view.ContinueBtn.onClick.AddListener(OnContinueClicked);

            if (_view.ExitBtn != null)
                _view.ExitBtn.onClick.AddListener(OnExitClicked);

            if (_view.LogoutBtn != null)
                _view.LogoutBtn.onClick.AddListener(OnLogoutClicked);

            if (_view.VolumeSlider != null)
            {
                _view.VolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                _view.VolumeSlider.SetValueWithoutNotify(_model.Volume);
            }

            if (_view.PerfToggle != null)
            {
                _view.PerfToggle.onValueChanged.AddListener(OnPerfToggleChanged);
                _view.PerfToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PERF_PREF_KEY, 1) == 1);
            }

            RefreshVolumeText();
        }

        protected override void OnHide()
        {
            if (_view == null) return;

            if (_view.ContinueBtn != null)
                _view.ContinueBtn.onClick.RemoveListener(OnContinueClicked);

            if (_view.ExitBtn != null)
                _view.ExitBtn.onClick.RemoveListener(OnExitClicked);

            if (_view.LogoutBtn != null)
                _view.LogoutBtn.onClick.RemoveListener(OnLogoutClicked);

            if (_view.VolumeSlider != null)
                _view.VolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

            if (_view.PerfToggle != null)
                _view.PerfToggle.onValueChanged.RemoveListener(OnPerfToggleChanged);
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<GameEndEvent>(OnGameEnd);
            if (s_Instance == this) s_Instance = null;
            base.OnDestroy();
        }

        private void OnGameEnd(GameEndEvent e)
        {
            // 对局结束时弹窗静默关闭（不触发恢复逻辑，HidePanel 内已有 IsGameOver 守卫）
            if (gameObject.activeSelf)
                HidePanel();
        }

        // ============================================================
        //  交互
        // ============================================================

        private void OnContinueClicked()
        {
            HidePanel();
        }

        private void OnExitClicked()
        {
            HidePanel();
            if (GameManager.HasInstance)
                GameManager.Instance.ExitToHome();
        }

        /// <summary>退出登录：若仍在房间先断开，清除令牌后关闭 UIHome 回到登录面板（主界面形态）</summary>
        private void OnLogoutClicked()
        {
            HidePanel();

            // 主界面即大厅：退出前断开可能持有的房间连接
            GameServerClient client = GameServerClient.Instance;
            if (client != null && client.IsConnected)
                client.Disconnect();

            if (AuthService.HasInstance)
                AuthService.Instance.Logout();

            UIManager.Instance.Pop();   // 关闭 UIHome，恢复显示 UILogin
        }

        private void OnVolumeChanged(float value)
        {
            _model.Volume = value;
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VOLUME_PREF_KEY, value);
            PlayerPrefs.Save();
            RefreshVolumeText();
        }

        /// <summary>性能信息开关：持久化并即时作用于 HUD</summary>
        private void OnPerfToggleChanged(bool value)
        {
            PlayerPrefs.SetInt(PERF_PREF_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            UIGameHudCtrl.RefreshPerfVisibility();
        }

        private void RefreshVolumeText()
        {
            if (_view != null && _view.VolumeValueText != null)
                _view.VolumeValueText.text = $"{Mathf.RoundToInt(_model.Volume * 100)}%";
        }
    }
}
