/// ============================================================
/// 文件名: ClickEffectInput.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 点击特效演示组件。挂载到场景任意对象后，鼠标点击屏幕任意位置
///       播放点击特效（默认随机轮播全部10种）。
///       正式接入 UI 时，按钮层应改调 ClickEffectFactory.Play 并用
///       EventSystem.IsPointerOverGameObject 过滤 UI 点击。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>
    /// 点击特效演示组件。
    /// 挂载到场景任意 GameObject 即生效：鼠标左键点击屏幕 → 在点击处播放特效。
    /// _playRandom=true 时随机轮播10种特效（演示/选型用）；
    /// 固定某种时置 false 并指定 _effectType。
    /// 引用：ClickEffectFactory.cs
    /// </summary>
    public class ClickEffectInput : MonoBehaviour
    {
        /// <summary>随机轮播全部特效（演示模式）</summary>
        [SerializeField] private bool _playRandom = true;

        /// <summary>固定播放的特效类型（_playRandom=false 时生效）</summary>
        [SerializeField] private ClickEffectType _effectType = ClickEffectType.RingPulse;

        /// <summary>目标相机（空则用 Camera.main）</summary>
        [SerializeField] private Camera _camera;

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            // ClickEffectType 共 10 种（WaterRipple=0 ~ WarmGlow=9）
            ClickEffectType type = _playRandom
                ? (ClickEffectType)Random.Range(0, 10)
                : _effectType;

            ClickEffectFactory.PlayAtScreenPoint(type, Input.mousePosition, _camera);
        }
    }
}
