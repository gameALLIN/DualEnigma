/// ============================================================
/// 文件名: UIGameOverCtrl.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 对局结算面板控制器。常驻 UILayer.Top（不进面板栈，仿 UIInvitePopup
///       模式）。对局自然结束（死亡/通关）时显示；手动退出对局
///       （ExitToHome 发布 isManualExit）不弹出。
///       [再来一局] 仅单机可用（联机需回房间重新开局）；[返回主界面] 走
///       GameManager.ExitToHome 统一出口。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIGameOverCtrl : UICtrlBase
    {
        /// <summary>预制体路径（相对于 AssetPackage/，与 UIManager 约定一致）</summary>
        private const string PREFAB_PATH = "Prefabs/UI/UIGameOver/UIGameOver";

        private static readonly Color32 VICTORY_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 DEFEAT_COLOR = new Color32(0xE5, 0x39, 0x35, 0xFF);
        private static readonly Color32 SUBTITLE_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF);

        private static UIGameOverCtrl s_Instance;

        private UIGameOverModel _model;
        private UIGameOverView _view;

        /// <summary>确保结算面板常驻实例存在（GameLaunch 启动时调用，幂等）</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;

            GameObject prefab = ResMgr.Instance.LoadPrefab(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[UIGameOver] 预制体加载失败: {PREFAB_PATH}（请先运行菜单 DualEnigma/UI/生成 UIGameOver 预制体）");
                return;
            }

            RectTransform layerRoot = UIManager.Instance.GetLayerRoot(UILayer.Top);
            if (layerRoot == null)
            {
                Debug.LogError("[UIGameOver] 未获取到 Top 层级根节点");
                return;
            }

            GameObject panelObj = Instantiate(prefab, layerRoot, false);
            panelObj.name = "UIGameOver";
            panelObj.transform.SetAsLastSibling();
            panelObj.SetActive(false); // 默认隐藏，对局自然结束时显示

            s_Instance = panelObj.GetComponent<UIGameOverCtrl>();
            if (s_Instance == null)
            {
                Debug.LogError("[UIGameOver] 预制体上未找到 UIGameOverCtrl 组件");
                Destroy(panelObj);
                return;
            }

            ((IUIPanel)s_Instance).OnCreate();
        }

        protected override void OnCreate()
        {
            _model = new UIGameOverModel();
            _view = GetComponent<UIGameOverView>();

            // 常驻面板：订阅挂在 OnCreate，销毁时注销
            EventBus.Instance.Subscribe<GameEndEvent>(OnGameEnd);
        }

        protected override void OnShow()
        {
            if (_view == null) return;

            if (_view.RestartBtn != null)
            {
                _view.RestartBtn.onClick.RemoveListener(OnRestartClicked);
                _view.RestartBtn.onClick.AddListener(OnRestartClicked);
            }

            if (_view.HomeBtn != null)
            {
                _view.HomeBtn.onClick.RemoveListener(OnHomeClicked);
                _view.HomeBtn.onClick.AddListener(OnHomeClicked);
            }
        }

        protected override void OnHide()
        {
            if (_view == null) return;

            if (_view.RestartBtn != null)
                _view.RestartBtn.onClick.RemoveListener(OnRestartClicked);

            if (_view.HomeBtn != null)
                _view.HomeBtn.onClick.RemoveListener(OnHomeClicked);
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<GameEndEvent>(OnGameEnd);
            if (s_Instance == this) s_Instance = null;
            base.OnDestroy();
        }

        // ============================================================
        //  对局结束 → 显示结算
        // ============================================================

        private void OnGameEnd(GameEndEvent e)
        {
            // 手动退出（设置面板【退出对局】/重连失败清理）不弹结算——玩家已主动选择离开
            if (e.isManualExit) return;
            if (gameObject.activeSelf) return;

            _model.IsVictory = e.isVictory;
            _model.IsNetworked = RoomSession.HasInstance && RoomSession.Instance.IsConnected;

            RefreshDisplay();
            gameObject.SetActive(true);
            ((IUIPanel)this).OnShow();
        }

        private void RefreshDisplay()
        {
            if (_view.TitleText != null)
            {
                _view.TitleText.text = _model.IsVictory ? "胜  利" : "失  败";
                _view.TitleText.color = _model.IsVictory ? VICTORY_COLOR : DEFEAT_COLOR;
            }

            if (_view.SubtitleText != null)
            {
                GameProgress p = GameManager.Instance.State.Progress;
                _view.SubtitleText.text = _model.IsVictory
                    ? $"旅途圆满 — 3章全部通关"
                    : $"止步于 第{p.Chapter}章 {p.Chapter}-{p.Section} · 第{p.Round}轮";
                _view.SubtitleText.color = SUBTITLE_COLOR;
            }

            // 再来一局：仅单机（联机需回房间由房主重新开局）
            if (_view.RestartBtn != null)
                _view.RestartBtn.gameObject.SetActive(!_model.IsNetworked);
        }

        // ============================================================
        //  交互
        // ============================================================

        /// <summary>再来一局（单机）：直接重开，无需回主界面</summary>
        private void OnRestartClicked()
        {
            HideInternal();
            GameManager.Instance.StartGame();
        }

        /// <summary>返回主界面：走统一出口（联机断连 + 恢复 UIHome）</summary>
        private void OnHomeClicked()
        {
            HideInternal();
            GameManager.Instance.ExitToHome();
        }

        private void HideInternal()
        {
            ((IUIPanel)this).OnHide();
            gameObject.SetActive(false);
        }
    }
}
