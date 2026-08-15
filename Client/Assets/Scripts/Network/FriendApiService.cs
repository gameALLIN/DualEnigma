/// ============================================================
/// 文件名: FriendApiService.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友与房间邀请 API 实现，通过 UnityWebRequest 调用
///       account-server 的 FriendController 端点。
/// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using DualEnigma.Framework.Core;
using DualEnigma.Data;

namespace DualEnigma.Network
{
    /// <summary>
    /// 好友与房间邀请 API 实现。
    /// 注册到 ServiceLocator，供 UI 层调用。
    /// Token 从 AuthService 取（登录后自动携带）。
    /// </summary>
    public class FriendApiService : Singleton<FriendApiService>, IFriendApiService
    {
        private NetworkConfig _config;
        private string _baseUrl;

        [Serializable]
        private class SendRequestPayload { public string username; }

        [Serializable]
        private class CreateInvitePayload { public long friendId; public string roomCode; }

        [Serializable]
        private class RoomCodeResponse { public string roomCode; }

        [Serializable]
        private class ErrorResponse { public string error; }

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IFriendApiService>(this);
            _config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");
            _baseUrl = _config != null ? _config.AccountServerUrl : "http://localhost:8081";
            Debug.Log($"[FriendApiService] 好友 API 初始化完成 ({_baseUrl})");
        }

        public void SendFriendRequest(string username,
            Action<FriendRequestData> onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(new SendRequestPayload { username = username });
            StartCoroutine(PostJson("/api/friends/requests", json,
                text =>
                {
                    FriendRequestData data = JsonUtility.FromJson<FriendRequestData>(text);
                    onSuccess?.Invoke(data);
                },
                onError));
        }

        public void GetFriendRequests(Action<List<FriendRequestData>> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetJson("/api/friends/requests",
                text => onSuccess?.Invoke(UnwrapArray<FriendRequestListWrapper, FriendRequestData>(text)),
                onError));
        }

        public void AcceptFriendRequest(long requestId, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(PutEmpty($"/api/friends/requests/{requestId}/accept", onSuccess, onError));
        }

        public void RejectFriendRequest(long requestId, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(PutEmpty($"/api/friends/requests/{requestId}/reject", onSuccess, onError));
        }

        public void GetFriends(Action<List<FriendData>> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetJson("/api/friends",
                text => onSuccess?.Invoke(UnwrapArray<FriendListWrapper, FriendData>(text)),
                onError));
        }

        public void RemoveFriend(long friendId, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(DeleteJson($"/api/friends/{friendId}", onSuccess, onError));
        }

        public void SearchUsers(string keyword, Action<List<FriendData>> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetJson($"/api/friends/search?keyword={Uri.EscapeDataString(keyword)}",
                text => onSuccess?.Invoke(UnwrapArray<FriendListWrapper, FriendData>(text)),
                onError));
        }

        public void CreateInvite(long friendId, string roomCode,
            Action<InviteData> onSuccess, Action<string> onError)
        {
            string json = JsonUtility.ToJson(new CreateInvitePayload
            {
                friendId = friendId,
                roomCode = roomCode
            });
            StartCoroutine(PostJson("/api/invites", json,
                text =>
                {
                    InviteData data = JsonUtility.FromJson<InviteData>(text);
                    onSuccess?.Invoke(data);
                },
                onError));
        }

        public void GetInvites(Action<List<InviteData>> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetJson("/api/invites",
                text => onSuccess?.Invoke(UnwrapArray<InviteListWrapper, InviteData>(text)),
                onError));
        }

        public void AcceptInvite(long inviteId, Action<string> onSuccess, Action<string> onError)
        {
            StartCoroutine(PutText($"/api/invites/{inviteId}/accept",
                text =>
                {
                    RoomCodeResponse resp = JsonUtility.FromJson<RoomCodeResponse>(text);
                    onSuccess?.Invoke(resp.roomCode);
                },
                onError));
        }

        public void DeclineInvite(long inviteId, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(PutEmpty($"/api/invites/{inviteId}/decline", onSuccess, onError));
        }

        // ============================================================
        //  请求辅助
        // ============================================================

        /// <summary>GET 请求（带 Token）</summary>
        private IEnumerator GetJson(string path, Action<string> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(_baseUrl + path))
            {
                AttachAuth(req);
                yield return req.SendWebRequest();
                HandleResponse(req, onSuccess, onError);
            }
        }

        /// <summary>POST JSON 请求（带 Token）</summary>
        private IEnumerator PostJson(string path, string json,
            Action<string> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest req = new UnityWebRequest(_baseUrl + path, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                AttachAuth(req);
                yield return req.SendWebRequest();
                HandleResponse(req, onSuccess, onError);
            }
        }

        /// <summary>PUT 空体请求（带 Token），成功回调携带响应文本</summary>
        private IEnumerator PutText(string path, Action<string> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest req = new UnityWebRequest(_baseUrl + path, "PUT"))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                AttachAuth(req);
                yield return req.SendWebRequest();
                HandleResponse(req, onSuccess, onError);
            }
        }

        private IEnumerator PutEmpty(string path, Action onSuccess, Action<string> onError)
        {
            yield return PutText(path, _ => onSuccess?.Invoke(), onError);
        }

        /// <summary>DELETE 请求（带 Token）</summary>
        private IEnumerator DeleteJson(string path, Action onSuccess, Action<string> onError)
        {
            using (UnityWebRequest req = UnityWebRequest.Delete(_baseUrl + path))
            {
                AttachAuth(req);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke();
                }
                else
                {
                    onError?.Invoke(ParseError(req));
                }
            }
        }

        /// <summary>附加 Bearer Token（未登录时不附加，服务端将返回 401）</summary>
        private static void AttachAuth(UnityWebRequest req)
        {
            if (AuthService.HasInstance && !string.IsNullOrEmpty(AuthService.Instance.Token))
            {
                req.SetRequestHeader("Authorization", "Bearer " + AuthService.Instance.Token);
            }
        }

        /// <summary>统一响应处理：2xx 成功，否则解析 error 字段</summary>
        private static void HandleResponse(UnityWebRequest req,
            Action<string> onSuccess, Action<string> onError)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(req.downloadHandler?.text ?? "{}");
            }
            else
            {
                onError?.Invoke(ParseError(req));
            }
        }

        private static string ParseError(UnityWebRequest req)
        {
            string body = req.downloadHandler?.text;
            if (!string.IsNullOrEmpty(body))
            {
                ErrorResponse err = null;
                try { err = JsonUtility.FromJson<ErrorResponse>(body); } catch { }
                if (err != null && !string.IsNullOrEmpty(err.error))
                    return err.error;
            }
            return $"请求失败 ({req.responseCode})";
        }

        /// <summary>JsonUtility 不支持裸数组，手动包一层 {items:[...]}；TWrapper 必须含 List&lt;T&gt; items 字段</summary>
        private static List<T> UnwrapArray<TWrapper, T>(string json) where TWrapper : class
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new List<T>();

            try
            {
                string wrapped = "{\"items\":" + json + "}";
                object wrapper = JsonUtility.FromJson(wrapped, typeof(TWrapper));
                // 通过反射取 items 字段（避免为三种包装各写一份重载）
                var field = typeof(TWrapper).GetField("items");
                return field?.GetValue(wrapper) as List<T> ?? new List<T>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FriendApiService] 数组解析失败: {e.Message}");
                return new List<T>();
            }
        }
    }
}
