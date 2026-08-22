/// ============================================================
/// 文件名: INetMessage.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 网络消息标记接口。实现类为可 JsonUtility 序列化的 DTO。
///       框架层零业务依赖：不包含任何具体消息定义。
/// 引用：NetMessageRegistry.cs, NetJson.cs
/// ============================================================

namespace DualEnigma.Framework.Network
{
    /// <summary>网络消息标记接口。实现类为可序列化 DTO（结构需与线上 JSON 一致）。</summary>
    public interface INetMessage
    {
    }
}
