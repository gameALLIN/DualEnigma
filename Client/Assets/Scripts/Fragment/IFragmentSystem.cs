/// ============================================================
/// 文件名: IFragmentSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 碎片系统服务接口。
/// ============================================================

namespace DualEnigma.Fragment
{
    /// <summary>
    /// 碎片系统服务接口，注册到 ServiceLocator。
    /// 引用：碎片系统.md §3.1
    /// </summary>
    public interface IFragmentSystem
    {
        /// <summary>当前场上存活的碎片数量</summary>
        int ActiveCount { get; }

        /// <summary>碎片被接住（由角色碰撞触发）</summary>
        void OnFragmentCollected(int fragmentId, byte playerId, bool isJumping);

        /// <summary>
        /// 查询碎片类型（包括已收集但未消耗的碎片）。
        /// 引用：合成系统.md §4.2 碎片验证消耗
        /// </summary>
        /// <param name="fragmentId">碎片唯一ID</param>
        /// <param name="type">输出的碎片类型</param>
        /// <returns>是否查找到对应碎片</returns>
        bool TryGetFragmentType(int fragmentId, out FragmentType type);
    }
}
