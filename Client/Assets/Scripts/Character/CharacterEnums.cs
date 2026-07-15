/// ============================================================
/// 文件名: CharacterEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色系统相关枚举定义。
/// ============================================================

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色类型。强制一水一火，不允许选相同角色。
    /// 引用：GDD v6.1 §2.1
    /// </summary>
    public enum CharacterType
    {
        /// <summary>水人 Aqua</summary>
        Aqua,
        /// <summary>火人 Ignis</summary>
        Ignis,
    }

    /// <summary>
    /// 动画状态枚举，用于网络同步。
    /// 引用：网络通信.md §3.2 高频状态同步
    /// </summary>
    public enum AnimState : byte
    {
        Idle,
        Run,
        Jump,
        Fall,
        Hurt,
    }
}
