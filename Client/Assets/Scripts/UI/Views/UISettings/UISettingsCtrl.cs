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

        private static UISettingsCtrl s_Instance;

        private UISettingsModel _model;
        private UISettingsView _view;

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

        /// <summary>打开设置弹窗（单机模式伴随暂停）</summary>
        public static void ShowPanel()
        {
            Ensure();
            if (s_Instance == null) return;
            if (s_Instance.gameObject.activeSelf) return;

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

            if (_view.ContinueBtn != null)
                _view.ContinueBtn.onClick.AddListener(OnContinueClicked);

            if (_view.ExitBtn != null)
                _view.ExitBtn.onClick.AddListener(OnExitClicked);

            if (_view.VolumeSlider != null)
            {
                _view.VolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                _view.VolumeSlider.SetValueWithoutNotify(_model.Volume);
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

            if (_view.VolumeSlider != null)
                _view.VolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
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

        private void OnVolumeChanged(float value)
        {
            _model.Volume = value;
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VOLUME_PREF_KEY, value);
            PlayerPrefs.Save();
            RefreshVolumeText();
        }

        private void RefreshVolumeText()
        {
            if (_view != null && _view.VolumeValueText != null)
                _view.VolumeValueText.text = $"{Mathf.RoundToInt(_model.Volume * 100)}%";
        }
    }
}
