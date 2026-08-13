/// ============================================================
/// 文件名: AuthService.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 账号认证服务实现，通过 HTTP REST API 与 account-server 通信。
/// ============================================================

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using DualEnigma.Framework.Core;
using DualEnigma.Data;

namespace DualEnigma.Network
{
    /// <summary>
    /// 账号认证服务实现。
    /// 通过 UnityWebRequest 调用 account-server 的 REST API。
    /// 注册到 ServiceLocator，供 UI 层调用。
    /// </summary>
    public class AuthService : Singleton<AuthService>, IAuthService
    {
        private NetworkConfig _config;
        private string _baseUrl;

        public bool IsLoggedIn { get; private set; }
        public string Token { get; private set; }
        public long AccountId { get; private set; }
        public string Username { get; private set; }
        public string DisplayName { get; private set; }

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IAuthService>(this);
            _config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");
            _baseUrl = _config != null ? _config.AccountServerUrl : "http://localhost:8081";
            Debug.Log($"[AuthService] 认证服务初始化完成 (API: {_baseUrl})");
        }

        public void Register(string username, string password, string displayName,
            Action<LoginResult> onSuccess, Action<string> onError)
        {
            StartCoroutine(RegisterCoroutine(username, password, displayName, onSuccess, onError));
        }

        public void Login(string username, string password,
            Action<LoginResult> onSuccess, Action<string> onError)
        {
            StartCoroutine(LoginCoroutine(username, password, onSuccess, onError));
        }

        public void Logout()
        {
            IsLoggedIn = false;
            Token = null;
            AccountId = 0;
            Username = null;
            DisplayName = null;
            Debug.Log("[AuthService] 已登出");
        }

        private IEnumerator RegisterCoroutine(string username, string password,
            string displayName, Action<LoginResult> onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(new RegisterPayload
            {
                username = username,
                password = password,
                displayName = string.IsNullOrEmpty(displayName) ? username : displayName
            });

            using (UnityWebRequest req = new UnityWebRequest($"{_baseUrl}/api/auth/register", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    LoginResponse resp = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
                    ApplyLoginResult(resp);
                    onSuccess?.Invoke(new LoginResult
                    {
                        token = resp.token,
                        accountId = resp.accountId,
                        username = resp.username,
                        displayName = resp.displayName
                    });
                }
                else
                {
                    string errorMsg = ParseErrorResponse(req.downloadHandler.text);
                    onError?.Invoke(errorMsg);
                }
            }
        }

        private IEnumerator LoginCoroutine(string username, string password,
            Action<LoginResult> onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(new LoginPayload
            {
                username = username,
                password = password
            });

            using (UnityWebRequest req = new UnityWebRequest($"{_baseUrl}/api/auth/login", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    LoginResponse resp = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
                    ApplyLoginResult(resp);
                    onSuccess?.Invoke(new LoginResult
                    {
                        token = resp.token,
                        accountId = resp.accountId,
                        username = resp.username,
                        displayName = resp.displayName
                    });
                }
                else
                {
                    string errorMsg = ParseErrorResponse(req.downloadHandler.text);
                    onError?.Invoke(errorMsg);
                }
            }
        }

        private void ApplyLoginResult(LoginResponse resp)
        {
            Token = resp.token;
            AccountId = resp.accountId;
            Username = resp.username;
            DisplayName = resp.displayName;
            IsLoggedIn = true;
            Debug.Log($"[AuthService] 登录成功: id={resp.accountId}, username={resp.username}");
        }

        private static string ParseErrorResponse(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return "网络请求失败";

            try
            {
                ErrorResponse err = JsonUtility.FromJson<ErrorResponse>(responseText);
                return !string.IsNullOrEmpty(err.error) ? err.error : "未知错误";
            }
            catch
            {
                return "服务器响应解析失败";
            }
        }

        // ── JSON 序列化结构体 ──

        [Serializable]
        private struct RegisterPayload
        {
            public string username;
            public string password;
            public string displayName;
        }

        [Serializable]
        private struct LoginPayload
        {
            public string username;
            public string password;
        }

        [Serializable]
        private struct LoginResponse
        {
            public string token;
            public long accountId;
            public string username;
            public string displayName;
        }

        [Serializable]
        private struct ErrorResponse
        {
            public string error;
        }
    }
}
