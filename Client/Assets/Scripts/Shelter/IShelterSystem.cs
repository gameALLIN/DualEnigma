/// ============================================================
/// 文件名: IShelterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 庇护系统服务接口。
/// ============================================================

using DualEnigma.Character;

namespace DualEnigma.Shelter
{
    /// <summary>
    /// 庇护系统服务接口，注册到 ServiceLocator。
    /// 引用：庇护系统.md §3.1
    /// </summary>
    public interface IShelterSystem
    {
        /// <summary>水人庇护能量值 (0-100)</summary>
        float AquaEnergy { get; }

        /// <summary>火人庇护能量值 (0-100)</summary>
        float IgnisEnergy { get; }

        /// <summary>水人HP</summary>
        int AquaHP { get; }

        /// <summary>火人HP</summary>
        int IgnisHP { get; }

        /// <summary>当前庇护环境</summary>
        ShelterEnvironment CurrentEnvironment { get; }

        /// <summary>设置当前庇护环境（阶段切换时调用）</summary>
        void SetEnvironment(ShelterEnvironment environment);

        /// <summary>每帧更新（由 GameManager 调用）</summary>
        void OnUpdate(float deltaTime);

        /// <summary>角色受伤</summary>
        void DealDamage(CharacterType target, int damage);

        /// <summary>角色治疗</summary>
        void Heal(CharacterType target, int amount);

        /// <summary>修改庇护参数（天赋系统调用）</summary>
        void ModifyParams(ShelterParams modifications);
    }
}
