/// ============================================================
/// 文件名: ICharacterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色系统服务接口。
/// ============================================================

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色系统服务接口，注册到 ServiceLocator。
    /// 引用：角色系统.md §3.1
    /// </summary>
    public interface ICharacterSystem
    {
        /// <summary>获取指定类型的角色</summary>
        CharacterController GetCharacter(CharacterType type);
    }
}
