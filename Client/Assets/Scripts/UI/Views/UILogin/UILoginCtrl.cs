/// ============================================================
/// 文件名: UILoginCtrl.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 登录面板控制器，处理注册/登录交互逻辑。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.UI;
using DualEnigma.Framework.Core;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UILoginCtrl : UICtrlBase
    {
        private UILoginModel _model;
        private UILoginView _view;
        private IAuthService _authService;

        protected override void OnCreate()
        {
            _model = new UILoginModel();
            _view = GetComponent<UILoginView>();
            _authService = ServiceLocator.Get<IAuthService>();

            if (_authService == null)
            {
                _ = AuthService.Instance;
                _authService = ServiceLocator.Get<IAuthService>();
            }

            BindEvents();
            ApplyMode();
        }

        private void BindEvents()
        {
            if (_view == null) return;

            if (_view.SubmitBtn != null)
                _view.SubmitBtn.onClick.AddListener(OnSubmitClicked);

            if (_view.ToggleModeBtn != null)
                _view.ToggleModeBtn.onClick.AddListener(OnToggleModeClicked);
        }

        private void UnbindEvents()
        {
            if (_view == null) return;

            if (_view.SubmitBtn != null)
                _view.SubmitBtn.onClick.RemoveListener(OnSubmitClicked);

            if (_view.ToggleModeBtn != null)
                _view.ToggleModeBtn.onClick.RemoveListener(OnToggleModeClicked);
        }

        private void OnToggleModeClicked()
        {
            var newMode = _model.Mode == UILoginModel.LoginMode.Login
                ? UILoginModel.LoginMode.Register
                : UILoginModel.LoginMode.Login;
            _model.SetMode(newMode);
            ApplyMode();
        }

        private void ApplyMode()
        {
            if (_view == null) return;
            bool isRegister = _model.Mode == UILoginModel.LoginMode.Register;
            _view.SetMode(isRegister);
            _view.SetError(null);
        }

        private void OnSubmitClicked()
        {
            if (_view == null || _authService == null) return;

            string username = _view.UsernameInput != null ? _view.UsernameInput.text.Trim() : "";
            string password = _view.PasswordInput != null ? _view.PasswordInput.text : "";

            if (string.IsNullOrEmpty(username))
            {
                _view.SetError("请输入用户名");
                return;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                _view.SetError("密码至少 6 位");
                return;
            }

            _model.SetLoading(true);
            _view.SetLoading(true);
            _view.SetError(null);

            if (_model.Mode == UILoginModel.LoginMode.Register)
            {
                string displayName = _view.DisplayNameInput != null ? _view.DisplayNameInput.text.Trim() : "";
                _authService.Register(username, password, displayName, OnAuthSuccess, OnAuthError);
            }
            else
            {
                _authService.Login(username, password, OnAuthSuccess, OnAuthError);
            }
        }

        private void OnAuthSuccess(LoginResult result)
        {
            _model.SetLoading(false);
            _view.SetLoading(false);
            Debug.Log($"[UILogin] 认证成功: {result.username} (id={result.accountId})");
            // TODO: 切换到大厅/主界面面板
        }

        private void OnAuthError(string error)
        {
            _model.SetLoading(false);
            _view.SetLoading(false);
            _view.SetError(error);
            Debug.LogWarning($"[UILogin] 认证失败: {error}");
        }

        protected override void OnHide()
        {
            UnbindEvents();
        }
    }
}
