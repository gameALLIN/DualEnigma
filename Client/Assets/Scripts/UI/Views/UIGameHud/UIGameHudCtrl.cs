/// ============================================================
/// 文件名: UIGameHudCtrl.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 局内 HUD 控制器。常驻 UILayer.Normal（不进面板栈，
///       仿 UIInvitePopup 模式），GameStart 显示 / GameEnd 隐藏。
///       节流轮询刷新血条/能量/碎片计数；阶段条事件驱动；
///       ESC 或设置按钮开关设置弹窗（单机伴随暂停）。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Fragment;
using DualEnigma.Shelter;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIGameHudCtrl : UICtrlBase
    {
        /// <summary>预制体路径（相对于 AssetPackage/，与 UIManager 约定一致）</summary>
        private const string PREFAB_PATH = "Prefabs/UI/UIGameHud/UIGameHud";

        /// <summary>数值轮询间隔（秒）</summary>
        private const float REFRESH_INTERVAL = 0.2f;

        /// <summary>低血量阈值（比例），低于后血条变红</summary>
        private const float LOW_HP_RATIO = 0.3f;

        /// <summary>性能信息开关的 PlayerPrefs 键（UISettings 弹窗写入）</summary>
        private const string PERF_PREF_KEY = "SettingsShowPerf";

        /// <summary>性能信息刷新窗口（秒），FPS 取窗口内平均</summary>
        private const float PERF_INTERVAL = 0.5f;

        private static UIGameHudCtrl s_Instance;

        private static readonly Color32 AQUA_HP_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 IGNIS_HP_COLOR = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 LOW_HP_COLOR = new Color32(0xE5, 0x39, 0x35, 0xFF);
        private static readonly Color32 ENERGY_COLOR = new Color32(0x26, 0xA6, 0x9A, 0xFF);

        private UIGameHudModel _model;
        private UIGameHudView _view;

        private float _refreshTimer;

        // 性能统计：FPS 窗口累计
        private int _frameCounter;
        private float _perfTimer;

        /// <summary>确保 HUD 常驻实例存在（GameLaunch 启动时调用，幂等）</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;

            GameObject prefab = ResMgr.Instance.LoadPrefab(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[UIGameHud] 预制体加载失败: {PREFAB_PATH}（请先运行菜单 DualEnigma/UI/生成 UIGameHUD 预制体）");
                return;
            }

            RectTransform layerRoot = UIManager.Instance.GetLayerRoot(UILayer.Normal);
            if (layerRoot == null)
            {
                Debug.LogError("[UIGameHud] 未获取到 Normal 层级根节点");
                return;
            }

            GameObject hudObj = Instantiate(prefab, layerRoot, false);
            hudObj.name = "UIGameHud";
            hudObj.transform.SetAsLastSibling();
            hudObj.SetActive(false); // 默认隐藏，对局开始才显示

            s_Instance = hudObj.GetComponent<UIGameHudCtrl>();
            if (s_Instance == null)
            {
                Debug.LogError("[UIGameHud] 预制体上未找到 UIGameHudCtrl 组件");
                Destroy(hudObj);
                return;
            }

            ((IUIPanel)s_Instance).OnCreate();
        }

        protected override void OnCreate()
        {
            _model = new UIGameHudModel();
            _view = GetComponent<UIGameHudView>();
            RefreshPerfVisibility();

            // 常驻面板：订阅挂在 OnCreate，销毁时注销
            EventBus.Instance.Subscribe<GameStartEvent>(OnGameStart);
            EventBus.Instance.Subscribe<GameEndEvent>(OnGameEnd);
            EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Instance.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Instance.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
        }

        protected override void OnShow()
        {
            if (_view != null && _view.SettingsBtn != null)
            {
                // 防重复绑定（StartGame 未配对 EndGame 重入时）
                _view.SettingsBtn.onClick.RemoveListener(OnSettingsClicked);
                _view.SettingsBtn.onClick.AddListener(OnSettingsClicked);
            }
        }

        protected override void OnHide()
        {
            if (_view != null && _view.SettingsBtn != null)
                _view.SettingsBtn.onClick.RemoveListener(OnSettingsClicked);
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
            {
                EventBus.Instance.Unsubscribe<GameStartEvent>(OnGameStart);
                EventBus.Instance.Unsubscribe<GameEndEvent>(OnGameEnd);
                EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
                EventBus.Instance.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
                EventBus.Instance.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            }
            if (s_Instance == this) s_Instance = null;
            base.OnDestroy();
        }

        // ============================================================
        //  事件驱动
        // ============================================================

        private void OnGameStart(GameStartEvent e)
        {
            _model.IsInGame = true;
            gameObject.SetActive(true);
            ((IUIPanel)this).OnShow();
            RefreshAll();
        }

        private void OnGameEnd(GameEndEvent e)
        {
            _model.IsInGame = false;
            ((IUIPanel)this).OnHide();
            gameObject.SetActive(false);
            // 返回主界面的出口由 UIGameOver 结算面板接管（死亡/通关弹结算，手动退出直接回主界面）
        }

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            // 阶段切换瞬间剩余时长即该阶段总时长（单机本地切换与联机 ApplyServerPhase 均如此）
            _model.PhaseTotalSeconds = Mathf.Max(0.5f, GameStateMachine.Instance.PhaseRemainingTime);
            RefreshPhaseTexts();
        }

        private void OnPlayerDamaged(PlayerDamagedEvent e)
        {
            RefreshVitals();
        }

        private void OnPlayerHealed(PlayerHealedEvent e)
        {
            RefreshVitals();
        }

        // ============================================================
        //  轮询刷新
        // ============================================================

        private void Update()
        {
            if (!_model.IsInGame) return;

            // ESC 开关设置弹窗
            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleSettings();

            // 倒计时文本与进度条每帧更新（数值轮询节流）
            RefreshPhaseTexts();

            // 性能信息：FPS 窗口累计 + RTT 读取（0.5s 更新一次文本）
            UpdatePerf();

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= REFRESH_INTERVAL)
            {
                _refreshTimer = 0f;
                RefreshAll();
            }
        }

        // ============================================================
        //  性能信息（FPS / 网络延迟）
        // ============================================================

        /// <summary>设置弹窗开关性能信息显示（静态供 UISettingsCtrl 调用）</summary>
        public static void RefreshPerfVisibility()
        {
            if (s_Instance == null || s_Instance._view == null || s_Instance._view.PerfText == null) return;
            bool show = PlayerPrefs.GetInt(PERF_PREF_KEY, 1) == 1;
            s_Instance._view.PerfText.gameObject.SetActive(show);
        }

        private void UpdatePerf()
        {
            if (_view == null || _view.PerfText == null) return;

            _frameCounter++;
            _perfTimer += Time.unscaledDeltaTime;
            if (_perfTimer < PERF_INTERVAL) return;

            int fps = _frameCounter > 0 ? Mathf.RoundToInt(_frameCounter / _perfTimer) : 0;
            _frameCounter = 0;
            _perfTimer = 0f;

            string text = $"FPS {fps}";

            // 联机时附带应用层 RTT（心跳往返，1Hz 更新）
            if (NetworkSystem.HasInstance && NetworkSystem.Instance.IsConnected
                && GameServerClient.HasInstance && GameServerClient.Instance.RttMs > 0f)
            {
                text += $"  PING {Mathf.RoundToInt(GameServerClient.Instance.RttMs)}ms";
            }

            _view.PerfText.text = text;
        }

        private void RefreshAll()
        {
            RefreshLevelInfo();
            RefreshVitals();
            RefreshFragmentCount();
            RefreshPhaseTexts();
        }

        private void RefreshLevelInfo()
        {
            if (_view.LevelInfoText == null) return;
            GameProgress p = GameManager.Instance.State.Progress;
            _view.LevelInfoText.text = $"第{p.Chapter}章 {p.Chapter}-{p.Section} · 第{p.Round}轮";
        }

        private void RefreshPhaseTexts()
        {
            if (_view.PhaseNameText == null) return;

            GameStateMachine sm = GameStateMachine.Instance;
            _view.PhaseNameText.text = GetPhaseName(sm.CurrentPhase);
            if (_view.PhaseTimerText != null)
                _view.PhaseTimerText.text = $"{Mathf.Max(0f, sm.PhaseRemainingTime):F1}s";

            if (_view.PhaseProgressFill != null)
                _view.PhaseProgressFill.fillAmount = Mathf.Clamp01(sm.PhaseRemainingTime / _model.PhaseTotalSeconds);
        }

        private void RefreshVitals()
        {
            if (_view.AquaHPFill == null) return;

            // 联机模式：对手数值以服务器 10Hz 快照为准（本地 ShelterSystem 对对手的模拟不可信）
            bool networked = NetworkSystem.HasInstance && NetworkSystem.Instance.IsConnected;
            byte localId = networked ? NetworkSystem.Instance.LocalPlayerId : (byte)0;

            int aquaHP = networked && localId != 0 ? NetworkSystem.Instance.OpponentHP : GameManager.Instance.AquaHP;
            int ignisHP = networked && localId != 1 ? NetworkSystem.Instance.OpponentHP : GameManager.Instance.IgnisHP;
            float aquaEnergy = networked && localId != 0 ? NetworkSystem.Instance.OpponentShelterEnergy : ShelterSystem.Instance.AquaEnergy;
            float ignisEnergy = networked && localId != 1 ? NetworkSystem.Instance.OpponentShelterEnergy : ShelterSystem.Instance.IgnisEnergy;

            ApplyVitals(_view.AquaHPFill, _view.AquaHPText, _view.AquaEnergyFill, _view.AquaEnergyText,
                aquaHP, aquaEnergy, AQUA_HP_COLOR);
            ApplyVitals(_view.IgnisHPFill, _view.IgnisHPText, _view.IgnisEnergyFill, _view.IgnisEnergyText,
                ignisHP, ignisEnergy, IGNIS_HP_COLOR);
        }

        private void ApplyVitals(Image hpFill, Text hpText, Image energyFill, Text energyText,
            int hp, float energy, Color32 normalColor)
        {
            const int MAX = 100;

            if (hpFill != null)
            {
                hpFill.fillAmount = Mathf.Clamp01(hp / (float)MAX);
                hpFill.color = hp / (float)MAX <= LOW_HP_RATIO ? LOW_HP_COLOR : normalColor;
            }
            if (hpText != null)
                hpText.text = $"{hp}/{MAX}";

            if (energyFill != null)
            {
                energyFill.color = ENERGY_COLOR;
                energyFill.fillAmount = Mathf.Clamp01(energy / MAX);
            }
            if (energyText != null)
                energyText.text = Mathf.RoundToInt(energy).ToString();
        }

        private void RefreshFragmentCount()
        {
            if (_view.FragmentCountText == null) return;

            int ice = 0, lava = 0, rock = 0;
            CountCarried(CharacterSystem.HasInstance ? CharacterSystem.Instance.Aqua : null, ref ice, ref lava, ref rock);
            CountCarried(CharacterSystem.HasInstance ? CharacterSystem.Instance.Ignis : null, ref ice, ref lava, ref rock);

            _view.FragmentCountText.text = $"冰×{ice}  熔岩×{lava}  岩石×{rock}";
        }

        private void CountCarried(DualEnigma.Character.CharacterController character, ref int ice, ref int lava, ref int rock)
        {
            if (character == null || character.Stats == null || !FragmentSystem.HasInstance) return;

            foreach (int id in character.Stats.CarriedFragmentIds)
            {
                if (FragmentSystem.Instance.TryGetFragmentType(id, out FragmentType type))
                {
                    switch (type)
                    {
                        case FragmentType.IceCrystal: ice++; break;
                        case FragmentType.Lava: lava++; break;
                        case FragmentType.Rock: rock++; break;
                    }
                }
            }
        }

        private string GetPhaseName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Preview: return "预告";
                case GamePhase.FragmentCollect: return "碎片收集";
                case GamePhase.DisasterPreview: return "灾害预告";
                case GamePhase.Build: return "建造";
                case GamePhase.DisasterImpact: return "灾害冲击";
                case GamePhase.Rest: return "修整";
                case GamePhase.Upgrade: return "升级";
                default: return phase.ToString();
            }
        }

        // ============================================================
        //  设置弹窗
        // ============================================================

        private void OnSettingsClicked()
        {
            ToggleSettings();
        }

        private void ToggleSettings()
        {
            if (UISettingsCtrl.IsVisible) UISettingsCtrl.HidePanel();
            else UISettingsCtrl.ShowPanel();
        }
    }
}
