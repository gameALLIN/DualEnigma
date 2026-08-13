/// ============================================================
/// 文件名: UILoginModel.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 登录面板数据模型。
/// ============================================================

using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UILoginModel : UIModelBase
    {
        public enum LoginMode { Login, Register }

        public LoginMode Mode { get; private set; } = LoginMode.Login;
        public bool IsLoading { get; private set; }
        public string ErrorMessage { get; private set; }

        public void SetMode(LoginMode mode)
        {
            Mode = mode;
            ErrorMessage = null;
            NotifyDataChanged();
        }

        public void SetLoading(bool loading)
        {
            IsLoading = loading;
            NotifyDataChanged();
        }

        public void SetError(string message)
        {
            ErrorMessage = message;
            IsLoading = false;
            NotifyDataChanged();
        }

        public void ClearError()
        {
            ErrorMessage = null;
            NotifyDataChanged();
        }
    }
}
