/// ============================================================
/// 文件名: CharacterStats.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色基础属性配置。
/// ============================================================

using System.Collections.Generic;

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色基础属性配置。
    /// 引用：GDD v6.1 §2.2 水人 / §2.3 火人
    /// </summary>
    [System.Serializable]
    public class CharacterStats
    {
        /// <summary>角色类型</summary>
        public CharacterType Type;
        /// <summary>最大生命值</summary>
        public int MaxHP = 100;
        /// <summary>当前生命值</summary>
        public int CurrentHP = 100;
        /// <summary>移动速度（格/秒，1格=1单位，PPU=32）</summary>
        public float MoveSpeed = 4f;
        /// <summary>跳跃高度（格），水人=2，火人=2（二段跳至3）</summary>
        public float JumpHeight = 2f;
        /// <summary>是否可二段跳（火人=true）</summary>
        public bool CanDoubleJump;
        /// <summary>搬运上限</summary>
        public int CarryLimit = 3;
        /// <summary>当前携带的碎片列表</summary>
        public List<int> CarriedFragmentIds = new List<int>();
    }
}
