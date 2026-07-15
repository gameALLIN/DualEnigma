/// ============================================================
/// 文件名: ISynthesisSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成系统服务接口。
/// ============================================================

using System.Collections.Generic;
using DualEnigma.Fragment;
using DualEnigma.Shelter;

namespace DualEnigma.Synthesis
{
    /// <summary>
    /// 合成系统服务接口，注册到 ServiceLocator。
    /// 引用：合成系统.md §3.1
    /// </summary>
    public interface ISynthesisSystem
    {
        /// <summary>当前庇护环境对应的合成表</summary>
        List<SynthesisRecipe> CurrentRecipes { get; }

        /// <summary>设置当前庇护环境</summary>
        void SetEnvironment(ShelterEnvironment environment);

        /// <summary>尝试开始合成</summary>
        SynthesisRecipe? TryStartSynthesis(byte playerId, FragmentType fragmentType);

        /// <summary>合成进度更新</summary>
        float GetSynthesisProgress(byte playerId);

        /// <summary>打断合成</summary>
        void InterruptSynthesis(byte playerId);
    }
}
