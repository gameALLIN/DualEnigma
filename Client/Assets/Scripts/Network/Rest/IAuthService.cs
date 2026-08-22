/// ============================================================
/// 文件名: IAuthService.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 账号认证服务接口，负责与 account-server REST API 通信。
/// ============================================================

namespace DualEnigma.Network
{
    /// <summary>
    /// 账号认证服务接口。
    /// 负责：注册、登录、Token 管理、账号信息查询。
    /// </summary>
    public interface IAuthService
    {
        /// <summary>当前是否已登录</summary>
        bool IsLoggedIn { get; }

        /// <summary>当前 JWT Token（未登录时为 null）</summary>
        string Token { get; }

        /// <summary>当前账号 ID（未登录时为 0）</summary>
        long AccountId { get; }

        /// <summary>当前用户名（未登录时为 null）</summary>
        string Username { get; }

        /// <summary>当前昵称（未登录时为 null）</summary>
        string DisplayName { get; }

        /// <summary>
        /// 注册账号（异步）。
        /// </summary>
        /// <param name="username">用户名（3-64 字符）</param>
        /// <param name="password">密码（6-128 字符）</param>
        /// <param name="displayName">昵称（可选）</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onError">失败回调（错误消息）</param>
        void Register(string username, string password, string displayName,
            System.Action<LoginResult> onSuccess, System.Action<string> onError);

        /// <summary>
        /// 登录（异步）。
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onError">失败回调（错误消息）</param>
        void Login(string username, string password,
            System.Action<LoginResult> onSuccess, System.Action<string> onError);

        /// <summary>
        /// 登出，清除本地 Token。
        /// </summary>
        void Logout();
    }

    /// <summary>
    /// 登录/注册成功返回结果。
    /// </summary>
    public struct LoginResult
    {
        public string token;
        public long accountId;
        public string username;
        public string displayName;
    }
}
