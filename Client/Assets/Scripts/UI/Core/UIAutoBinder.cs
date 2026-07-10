using UnityEngine;

namespace DualEnigma.UI
{
    /// <summary>
    /// UI 组件自动绑定脚本，挂载在 UI 面板预制体根节点上。
    /// 作为占位组件，实际绑定逻辑由 Editor 工具 UIBindingGenerator 执行。
    /// </summary>
    public class UIAutoBinder : MonoBehaviour
    {
        /// <summary>该面板对应的 View 脚本类型名（如 "UIHomeView"），留空时自动检测</summary>
        [SerializeField] private string m_ViewTypeName = string.Empty;

        /// <summary>获取或设置关联的 View 脚本类型名</summary>
        public string ViewTypeName
        {
            get => m_ViewTypeName;
            set => m_ViewTypeName = value;
        }
    }
}
