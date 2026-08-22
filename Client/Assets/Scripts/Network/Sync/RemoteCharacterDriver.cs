/// ============================================================
/// 文件名: RemoteCharacterDriver.cs
/// 创建时间: 2026-08-16
/// 最后更新: 2026-08-18
/// 作者: DualEnigma
/// 描述: 远程角色驱动器。订阅 HighFreqStateReceivedEvent，
///       以插值缓冲（默认 100ms）回放对方位置，平滑网络抖动。
///       包流停滞 >500ms 进入外推模式（最后速度外推，上限 100ms，
///       对齐同步策略.md §2.3）；外推耗尽进入失联态：半透明 +
///       头顶失联图标 + 手动积分重力模拟落地（根治空中断线悬空）；
///       恢复收包后 0.2s 内平滑吸附回权威位置。
///       组件挂在远程角色上（Kinematic 刚体，不参与本地物理模拟）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Data;
using DualEnigma.Network;
using DualEnigma.Art;

namespace DualEnigma.Character
{
    [RequireComponent(typeof(CharacterController))]
    public class RemoteCharacterDriver : MonoBehaviour
    {
        /// <summary>驱动状态：正常插值 → 停滞外推 → 失联（可逆，恢复收包即回插值）</summary>
        private enum DriverState : byte
        {
            Interpolating = 0,
            Extrapolating = 1,
            Disconnected = 2
        }

        private struct Sample
        {
            public float Time;
            public Vector2 Position;
        }

        /// <summary>Ground 层索引（静态缓存，与 CharacterController 同一来源）</summary>
        private static int _groundLayer = -1;

        private CharacterController _controller;
        private Rigidbody2D _rb;
        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private readonly List<Sample> _buffer = new List<Sample>();

        private float _bufferSec = 0.1f;
        private float _stallThreshold = 0.5f;
        private float _maxExtrapolationTime = 0.1f;
        private float _disconnectedAlpha = 0.5f;
        private float _resnapDuration = 0.2f;

        private string _lastAnim = "";
        private bool _lastFacing = true;

        private DriverState _state = DriverState.Interpolating;

        /// <summary>最近一次收包时刻（Time.time，到达时刻打点）；-1 = 尚未收到过包</summary>
        private float _lastSampleTime = -1f;

        /// <summary>最近样本携带的速度（外推用）</summary>
        private Vector2 _lastVelocity;

        // ── 外推模式 ──
        private Vector2 _extrapolationOrigin;
        private float _extrapolationStartTime;

        // ── 失联模拟下落 ──
        private float _fallSpeed;
        private bool _landed;
        private const float LAND_SKIN = 0.05f;

        // ── 重连吸附 ──
        private bool _resnapActive;
        private float _resnapTimer;
        private Vector2 _resnapFrom;

        // ── 失联表现 ──
        private Color _originalSpriteColor = Color.white;
        private SpriteRenderer _disconnectIcon;
        private Texture2D _iconTexture;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_groundLayer < 0)
                _groundLayer = LayerMask.NameToLayer("Ground");

            if (_spriteRenderer != null)
                _originalSpriteColor = _spriteRenderer.color;

            NetworkConfig config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");
            if (config != null)
            {
                _bufferSec = config.InterpolationBuffer;
                _stallThreshold = config.StallThreshold;
                _maxExtrapolationTime = config.MaxExtrapolationTime;
                _disconnectedAlpha = config.DisconnectedAlpha;
                _resnapDuration = config.ResnapDuration;
            }
        }

        private void OnEnable()
        {
            if (EventBus.HasInstance)
                EventBus.Instance.Subscribe<HighFreqStateReceivedEvent>(OnStateReceived);
        }

        private void OnDisable()
        {
            // 场景卸载/重建角色时 EventBus 单例可能已销毁（Instance getter 会 NRE）
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<HighFreqStateReceivedEvent>(OnStateReceived);
        }

        private void OnDestroy()
        {
            // 程序化生成的图标资源随组件销毁释放（零外部资源，运行时创建）
            if (_disconnectIcon != null)
            {
                Destroy(_disconnectIcon.sprite);
                _disconnectIcon = null;
            }
            if (_iconTexture != null)
            {
                Destroy(_iconTexture);
                _iconTexture = null;
            }
        }

        private void OnStateReceived(HighFreqStateReceivedEvent e)
        {
            if (_controller == null || e.playerId != _controller.PlayerId) return;

            // 外推/失联后恢复收包 → 清空过期缓冲重新起步，并从当前渲染位置平滑吸附
            if (_state != DriverState.Interpolating)
            {
                _buffer.Clear();
                _state = DriverState.Interpolating;
                _landed = false;
                _fallSpeed = 0f;

                _resnapActive = true;
                _resnapTimer = 0f;
                _resnapFrom = _rb != null ? _rb.position : e.position;

                ApplyDisconnectVisual(false);
            }

            _lastSampleTime = Time.time;
            _lastVelocity = e.velocity;

            _buffer.Add(new Sample { Time = Time.time, Position = e.position });
            if (_buffer.Count > 40) _buffer.RemoveAt(0); // ~2s @20Hz 上限，防内存增长

            if (e.animState != _lastAnim)
            {
                _lastAnim = e.animState;
                if (System.Enum.TryParse(_lastAnim, out AnimState anim))
                    _controller.SetNetworkAnimState(anim);
            }

            if (e.facing != _lastFacing)
            {
                _lastFacing = e.facing;
                _controller.SetNetworkFacing(e.facing);
            }
        }

        private void Update()
        {
            if (_rb == null) return;

            switch (_state)
            {
                case DriverState.Extrapolating:
                    UpdateExtrapolating();
                    break;
                case DriverState.Disconnected:
                    UpdateDisconnected();
                    break;
                default:
                    UpdateInterpolating();
                    break;
            }
        }

        // ============================================================
        //  正常插值（含停滞检测与重连吸附）
        // ============================================================

        private void UpdateInterpolating()
        {
            // 停滞检测：超过阈值未收到新包 → 进入外推模式（尚未收到过包时不触发）
            if (_lastSampleTime > 0f && Time.time - _lastSampleTime > _stallThreshold)
            {
                EnterExtrapolation();
                UpdateExtrapolating();
                return;
            }

            if (_buffer.Count == 0) return;

            float renderTime = Time.time - _bufferSec;

            // 丢弃被追上的过期样本（保留最近 2 个用于插值）
            while (_buffer.Count > 2 && _buffer[1].Time <= renderTime)
                _buffer.RemoveAt(0);

            Vector2 target;
            if (_buffer.Count >= 2)
            {
                Sample a = _buffer[0];
                Sample b = _buffer[1];
                float span = b.Time - a.Time;
                float t = span > 0f ? Mathf.Clamp01((renderTime - a.Time) / span) : 1f;
                target = Vector2.Lerp(a.Position, b.Position, t);
            }
            else
            {
                // 恢复初期仅 1 个样本：直接以最新权威位置为目标
                target = _buffer[_buffer.Count - 1].Position;
            }

            if (_resnapActive)
            {
                // 重连吸附：从失联时渲染位置在吸附时长内 Lerp 到权威插值结果（目标可移动）
                _resnapTimer += Time.deltaTime;
                float k = _resnapDuration > 0f ? Mathf.Clamp01(_resnapTimer / _resnapDuration) : 1f;
                _rb.position = Vector2.Lerp(_resnapFrom, target, k);
                if (k >= 1f) _resnapActive = false;
            }
            else
            {
                _rb.position = target;
            }
        }

        // ============================================================
        //  停滞外推（上限 MaxExtrapolationTime，默认 100ms）
        // ============================================================

        private void EnterExtrapolation()
        {
            _state = DriverState.Extrapolating;
            _extrapolationOrigin = _rb.position;
            _extrapolationStartTime = Time.time;
        }

        private void UpdateExtrapolating()
        {
            float elapsed = Time.time - _extrapolationStartTime;
            if (elapsed >= _maxExtrapolationTime)
            {
                // 外推预算耗尽仍无包 → 失联态
                EnterDisconnected();
                UpdateDisconnected();
                return;
            }
            _rb.position = _extrapolationOrigin + _lastVelocity * elapsed;
        }

        // ============================================================
        //  失联态：手动积分重力模拟下落，Raycast 检测 Ground 层落地即停
        // ============================================================

        private void EnterDisconnected()
        {
            _state = DriverState.Disconnected;
            _landed = false;
            // 保留向下速度分量：上升中则从 0 开始下落，下落中则延续当前速度
            _fallSpeed = Mathf.Min(0f, _lastVelocity.y);
            ApplyDisconnectVisual(true);
        }

        private void UpdateDisconnected()
        {
            if (_landed) return;

            float dt = Time.deltaTime;

            // Kinematic 刚体无物理模拟，手动积分重力（含 gravityScale）
            _fallSpeed += Physics2D.gravity.y * _rb.gravityScale * dt;

            Vector2 pos = _rb.position;
            float nextY = pos.y + _fallSpeed * dt;

            // 从中心向下射线：覆盖半身高度 + 本帧下落位移 + 落地皮肤
            float halfHeight = _collider != null ? _collider.bounds.extents.y : 0.9f;
            float rayLen = Mathf.Max(0f, pos.y - nextY) + halfHeight + LAND_SKIN;
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, rayLen, 1 << _groundLayer);
            if (hit.collider != null)
            {
                _rb.position = new Vector2(pos.x, hit.point.y + halfHeight + LAND_SKIN);
                _fallSpeed = 0f;
                _landed = true; // 落地即停，不再移动（等待重连吸附恢复）
                return;
            }

            _rb.position = new Vector2(pos.x, nextY);
        }

        // ============================================================
        //  失联表现：半透明 + 头顶程序化失联图标（零外部资源）
        // ============================================================

        private void ApplyDisconnectVisual(bool disconnected)
        {
            if (_spriteRenderer != null)
            {
                Color c = _originalSpriteColor;
                c.a = disconnected ? _disconnectedAlpha : _originalSpriteColor.a;
                _spriteRenderer.color = c;
            }

            if (disconnected) EnsureDisconnectIcon();
            if (_disconnectIcon != null)
                _disconnectIcon.gameObject.SetActive(disconnected);
        }

        /// <summary>懒创建头顶失联图标（红底白叹号，程序化生成，仅首次失联时创建）</summary>
        private void EnsureDisconnectIcon()
        {
            if (_disconnectIcon != null) return;

            GameObject iconObj = new GameObject("DisconnectIcon");
            iconObj.transform.SetParent(transform, false);

            _disconnectIcon = iconObj.AddComponent<SpriteRenderer>();
            _iconTexture = BuildDisconnectIconTexture();
            _disconnectIcon.sprite = ProceduralSpriteGenerator.TextureToSprite(_iconTexture);
            _disconnectIcon.sortingOrder = 10; // 确保绘制在角色 Sprite 之上
            _disconnectIcon.gameObject.SetActive(false);

            float halfHeight = _collider != null ? _collider.bounds.extents.y : 0.9f;
            iconObj.transform.localPosition = new Vector3(0f, halfHeight + 0.35f, 0f);
            iconObj.transform.localScale = Vector3.one * 0.5f;
        }

        /// <summary>生成 32x32 失联图标纹理：红色圆底 + 白色叹号</summary>
        private static Texture2D BuildDisconnectIconTexture()
        {
            const int SIZE = 32;
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(SIZE, SIZE);
            Color red = new Color32(0xE5, 0x39, 0x35, 0xFF);
            Color white = Color.white;

            ProceduralSpriteGenerator.DrawSolidCircle(tex, 16, 16, 14f, red);
            // 叹号竖条（含上下端点像素，FillRect 边界含端）
            ProceduralSpriteGenerator.FillRect(tex, 14, 12, 17, 22, white);
            // 叹号圆点
            ProceduralSpriteGenerator.FillRect(tex, 14, 6, 17, 9, white);

            tex.Apply();
            return tex;
        }
    }
}
