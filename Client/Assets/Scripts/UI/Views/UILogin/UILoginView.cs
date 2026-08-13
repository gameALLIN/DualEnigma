/// ============================================================
/// 文件名: UILoginView.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 登录面板视图，持有 UGUI 组件引用。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.UI;

namespace DualEnigma.UI
{
    public class UILoginView : UIViewBase
    {
        // ===== Auto Bind Fields（自动绑定，请勿手动修改）=====
        [Header("输入框")]
        [SerializeField] private InputField m_UsernameInput;
        [SerializeField] private InputField m_PasswordInput;
        [SerializeField] private InputField m_DisplayNameInput;

        [Header("按钮")]
        [SerializeField] private Button m_SubmitBtn;
        [SerializeField] private Button m_ToggleModeBtn;

        [Header("文本")]
        [SerializeField] private Text m_TitleText;
        [SerializeField] private Text m_SubmitBtnText;
        [SerializeField] private Text m_ToggleModeBtnText;
        [SerializeField] private Text m_ErrorText;

        [Header("容器")]
        [SerializeField] private GameObject m_DisplayNameGroup;
        [SerializeField] private GameObject m_LoadingGroup;
        // ===== Auto Bind End =====

        public InputField UsernameInput => m_UsernameInput;
        public InputField PasswordInput => m_PasswordInput;
        public InputField DisplayNameInput => m_DisplayNameInput;
        public Button SubmitBtn => m_SubmitBtn;
        public Button ToggleModeBtn => m_ToggleModeBtn;
        public Text TitleText => m_TitleText;
        public Text SubmitBtnText => m_SubmitBtnText;
        public Text ToggleModeBtnText => m_ToggleModeBtnText;
        public Text ErrorText => m_ErrorText;
        public GameObject DisplayNameGroup => m_DisplayNameGroup;
        public GameObject LoadingGroup => m_LoadingGroup;

        public void SetLoading(bool loading)
        {
            if (m_SubmitBtn != null) m_SubmitBtn.interactable = !loading;
            if (m_ToggleModeBtn != null) m_ToggleModeBtn.interactable = !loading;
            if (m_LoadingGroup != null) m_LoadingGroup.SetActive(loading);
        }

        public void SetMode(bool isRegister)
        {
            if (m_TitleText != null) m_TitleText.text = isRegister ? "注册账号" : "登录";
            if (m_SubmitBtnText != null) m_SubmitBtnText.text = isRegister ? "注册" : "登录";
            if (m_ToggleModeBtnText != null) m_ToggleModeBtnText.text = isRegister ? "已有账号？去登录" : "没有账号？去注册";
            if (m_DisplayNameGroup != null) m_DisplayNameGroup.SetActive(isRegister);
        }

        public void SetError(string message)
        {
            if (m_ErrorText != null)
            {
                m_ErrorText.text = message ?? "";
                m_ErrorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }
    }
}
