/// ============================================================
/// 文件名: FriendItem.cs
/// 创建时间: 2026-08-18
/// 最后更新: 2026-08-21
/// 作者: DualEnigma
/// 描述: 好友条目通用组件（水平列表容器驱动）。行内子控件由
///       HorizontalLayoutGroup + LayoutElement 排布：昵称列弹性占满、
///       状态/ID/按钮按首选宽度右聚；隐藏列（如紧凑模式 ID）自动回流。
///       三种模式复用同一结构：Friend 好友行 / Search 搜索结果行 /
///       Request 好友申请行 / Invite 邀请行。
///       双形态：① 预制体形态（Common/FriendItem.prefab，序列化绑定）；
///       ② 纯代码形态（FriendItem.Create() 懒建兜底）。
/// ============================================================

using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    /// <summary>条目模式：决定列显隐、按钮文案与语义</summary>
    public enum FriendItemMode : byte
    {
        /// <summary>好友列表行：昵称/状态/ID + 邀请 + 删除</summary>
        Friend = 0,
        /// <summary>搜索结果行：昵称/ID + 添加（无状态、无删除）</summary>
        Search = 1,
        /// <summary>好友申请行：昵称 + 副文本 + 接受 + 拒绝</summary>
        Request = 2,
        /// <summary>邀请行（邀请抽屉）：昵称/状态 + 邀请（无 ID、无删除）</summary>
        Invite = 3
    }

    /// <summary>
    /// 好友条目。SetMode() 切换形态，BindFriend()/BindRequest() 填充数据，
    /// SetCompactLayout(width) 切换紧凑形态（窄容器），
    /// 按钮交互由宿主 Ctrl 通过 PrimaryBtn/SecondaryBtn 自行挂接。
    /// </summary>
    public sealed class FriendItem : MonoBehaviour
    {
        // ── 统一风格常量（与 GenerateUIFriendsPrefab 现行配色一致） ──
        private static readonly Color32 PRIMARY_BTN_COLOR = new Color32(0x4F, 0xC3, 0xF7, 0xFF); // 主按钮（邀请/接受/添加）
        private static readonly Color32 SECONDARY_BTN_COLOR = new Color32(0x37, 0x47, 0x4F, 0xFF); // 次按钮（拒绝/忽略）
        private static readonly Color32 DANGER_BTN_COLOR = new Color32(0xEF, 0x53, 0x50, 0xFF); // 删除
        private static readonly Color32 FRIEND_ROW_BG = new Color32(0x37, 0x47, 0x4F, 0xFF); // 好友行底色
        private static readonly Color32 REQUEST_ROW_BG = new Color32(0x2E, 0x3D, 0x45, 0xFF); // 申请行底色
        private static readonly Color32 LABEL_COLOR = new Color32(0xB0, 0xBE, 0xC5, 0xFF); // 次要文字

        // ── 在线四态配色 ──
        private static readonly Color32 STATUS_ONLINE = new Color32(0x66, 0xBB, 0x6A, 0xFF);
        private static readonly Color32 STATUS_TEAMING = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 STATUS_INGAME = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 STATUS_OFFLINE = new Color32(0x78, 0x90, 0x9C, 0xFF);

        private static Font _builtinFont;

        /// <summary>行尺寸（标准宽度；紧凑形态由 SetCompactLayout 指定）</summary>
        public const float ROW_WIDTH = 656f;
        public const float ROW_HEIGHT = 34f;

        // ── 子控件（预制体形态：序列化绑定；纯代码形态：BuildChildren 懒建赋值） ──
        [SerializeField] private Image m_Bg;
        [SerializeField] private Text m_NameText;
        [SerializeField] private Text m_StatusText;   // 好友模式=在线状态；申请模式=副文本
        [SerializeField] private Text m_IdText;       // 好友/搜索模式；紧凑/邀请/申请隐藏
        [SerializeField] private Button m_PrimaryBtn;   // 邀请 / 添加 / 接受
        [SerializeField] private Button m_SecondaryBtn; // 删除（隐藏）/ 忽略（隐藏）/ 拒绝

        // ── 子控件 LayoutElement 缓存（水平列表容器排布用） ──
        private LayoutElement m_NameLE;
        private LayoutElement m_StatusLE;
        private LayoutElement m_IdLE;
        private LayoutElement m_PrimaryLE;
        private LayoutElement m_SecondaryLE;

        /// <summary>紧凑布局标记（窄容器如主界面邀请抽屉）：隐藏 ID 列、昵称仅显示昵称本体</summary>
        private bool _compact;

        /// <summary>当前模式</summary>
        public FriendItemMode Mode => _mode;
        private FriendItemMode _mode = FriendItemMode.Friend;

        public Text NameText => m_NameText;
        public Button PrimaryBtn => m_PrimaryBtn;
        public Button SecondaryBtn => m_SecondaryBtn;

        // ============================================================
        //  构建
        // ============================================================

        /// <summary>
        /// 程序化创建一条好友条目。width 传窄宽度（如抽屉 296）自动切换紧凑布局。
        /// </summary>
        public static FriendItem Create(Transform parent, string objectName = "FriendItem",
            float width = ROW_WIDTH)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, ROW_HEIGHT);

            FriendItem item = root.AddComponent<FriendItem>();
            item.BuildChildren();
            if (width > 0f && Mathf.Abs(width - ROW_WIDTH) > 0.5f)
                item.SetCompactLayout(width);
            item.SetMode(FriendItemMode.Friend);
            return item;
        }

        /// <summary>子控件缺失时程序化补建（纯代码形态 / 预制体漏绑兜底）；并缓存 LayoutElement。
        /// m_Bg 允许绑到根节点自身 Image（spec 绑定器只搜子节点，自引用由这里兜底），
        /// 其余子控件任一缺失才整体重建，避免预制体形态下重复创建子节点。</summary>
        private void EnsureControls()
        {
            if (m_Bg == null) m_Bg = GetComponent<Image>();

            if (m_NameText == null || m_StatusText == null || m_IdText == null ||
                m_PrimaryBtn == null || m_SecondaryBtn == null)
            {
                BuildChildren();
            }
            CacheLayoutElements();
        }

        /// <summary>从已就绪的子控件缓存 LayoutElement（纯代码与预制体克隆两种形态通用）</summary>
        private void CacheLayoutElements()
        {
            if (m_NameLE == null && m_NameText != null)
                m_NameLE = m_NameText.GetComponent<LayoutElement>();
            if (m_StatusLE == null && m_StatusText != null)
                m_StatusLE = m_StatusText.GetComponent<LayoutElement>();
            if (m_IdLE == null && m_IdText != null)
                m_IdLE = m_IdText.GetComponent<LayoutElement>();
            if (m_PrimaryLE == null && m_PrimaryBtn != null)
                m_PrimaryLE = m_PrimaryBtn.GetComponent<LayoutElement>();
            if (m_SecondaryLE == null && m_SecondaryBtn != null)
                m_SecondaryLE = m_SecondaryBtn.GetComponent<LayoutElement>();
        }

        private void BuildChildren()
        {
            if (_builtinFont == null)
                _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 复用根节点已有 Image（预制体形态），避免重复挂图
            m_Bg = GetComponent<Image>();
            if (m_Bg == null) m_Bg = gameObject.AddComponent<Image>();
            m_Bg.color = FRIEND_ROW_BG;

            ApplyLayoutSettings(GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>());

            m_NameText = CreateText("NameText", "", 15, new Vector2(240f, 26f), Color.white,
                TextAnchor.MiddleLeft, flexibleWidth: 1f);

            m_StatusText = CreateText("StatusText", "", 13, new Vector2(120f, 24f), LABEL_COLOR,
                TextAnchor.MiddleLeft);

            m_IdText = CreateText("IdText", "", 13, new Vector2(120f, 24f), LABEL_COLOR,
                TextAnchor.MiddleRight);

            m_PrimaryBtn = CreateButton("PrimaryBtn", "邀请", PRIMARY_BTN_COLOR,
                new Vector2(64f, 26f), 13);

            m_SecondaryBtn = CreateButton("SecondaryBtn", "删除", DANGER_BTN_COLOR,
                new Vector2(48f, 26f), 13);
        }

        /// <summary>标准形态水平列表容器设置：左对齐、内容垂直居中</summary>
        private static void ApplyLayoutSettings(HorizontalLayoutGroup layout)
        {
            layout.padding = new RectOffset(12, 12, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // ============================================================
        //  紧凑形态
        // ============================================================

        /// <summary>
        /// 紧凑布局（窄容器如邀请抽屉）：行宽收窄 + 压缩首选宽度/字号 + 间距收紧。
        /// ID 列由 SetMode 依据 _compact 自动隐藏回流。
        /// </summary>
        public void SetCompactLayout(float width)
        {
            EnsureControls();
            _compact = true;

            GetComponent<RectTransform>().sizeDelta = new Vector2(width, ROW_HEIGHT);

            HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(10, 10, 0, 0);
                layout.spacing = 6f;
            }

            if (m_NameText != null) m_NameText.fontSize = 13;
            if (m_NameLE != null) m_NameLE.preferredWidth = 90f;

            if (m_StatusText != null) m_StatusText.fontSize = 11;
            if (m_StatusLE != null) m_StatusLE.preferredWidth = 64f;

            if (m_PrimaryLE != null) m_PrimaryLE.preferredWidth = 48f;
            if (m_PrimaryBtn != null)
            {
                Text label = m_PrimaryBtn.GetComponentInChildren<Text>();
                if (label != null) label.fontSize = 12;
            }

            if (m_SecondaryLE != null) m_SecondaryLE.preferredWidth = 44f;
        }

        // ============================================================
        //  模式与数据
        // ============================================================

        /// <summary>切换条目形态（列显隐 + 按钮文案 + 底色；布局容器自动回流）</summary>
        public void SetMode(FriendItemMode mode)
        {
            EnsureControls();
            _mode = mode;

            switch (mode)
            {
                case FriendItemMode.Friend:
                    m_Bg.color = FRIEND_ROW_BG;
                    m_NameText.gameObject.SetActive(true);
                    m_StatusText.gameObject.SetActive(true);
                    m_IdText.gameObject.SetActive(!_compact);
                    SetPrimary("邀请", PRIMARY_BTN_COLOR);
                    SetSecondary("删除", DANGER_BTN_COLOR, true);
                    break;

                case FriendItemMode.Search:
                    m_Bg.color = FRIEND_ROW_BG;
                    m_NameText.gameObject.SetActive(true);
                    m_StatusText.gameObject.SetActive(false);
                    m_IdText.gameObject.SetActive(!_compact);
                    SetPrimary("添加", PRIMARY_BTN_COLOR);
                    SetSecondary("", SECONDARY_BTN_COLOR, false);
                    break;

                case FriendItemMode.Request:
                    m_Bg.color = REQUEST_ROW_BG;
                    m_NameText.gameObject.SetActive(true);
                    m_StatusText.gameObject.SetActive(true); // 复用为副文本
                    m_IdText.gameObject.SetActive(false);
                    SetPrimary("接受", PRIMARY_BTN_COLOR);
                    SetSecondary("拒绝", SECONDARY_BTN_COLOR, true);
                    break;

                case FriendItemMode.Invite:
                    m_Bg.color = FRIEND_ROW_BG;
                    m_NameText.gameObject.SetActive(true);
                    m_StatusText.gameObject.SetActive(true);
                    m_IdText.gameObject.SetActive(!_compact);
                    SetPrimary("邀请", PRIMARY_BTN_COLOR);
                    SetSecondary("", SECONDARY_BTN_COLOR, false);
                    break;
            }
        }

        /// <summary>填充好友/搜索数据（状态列按模式自动处理）</summary>
        public void BindFriend(FriendData friend)
        {
            EnsureControls();
            if (friend == null) return;

            // 紧凑形态只显示昵称本体；标准形态显示 昵称 (用户名)
            m_NameText.text = _compact ? friend.displayName : $"{friend.displayName} ({friend.username})";
            if (m_IdText.gameObject.activeSelf)
                m_IdText.text = "ID: " + friend.accountId;

            if (_mode == FriendItemMode.Friend || _mode == FriendItemMode.Invite)
            {
                ApplyStatus(friend.status);
                // 游戏中无法接受邀请，置灰
                m_PrimaryBtn.interactable = friend.status != "ingame";
            }
        }

        /// <summary>填充好友申请数据（昵称 + 请求副文本）</summary>
        public void BindRequest(FriendRequestData request)
        {
            EnsureControls();
            if (request == null) return;

            m_NameText.text = request.fromDisplayName;
            m_StatusText.text = "请求加你为好友";
            m_StatusText.color = LABEL_COLOR;
        }

        /// <summary>在线状态四态渲染（好友/邀请模式）</summary>
        private void ApplyStatus(string status)
        {
            switch (status)
            {
                case "online":
                    m_StatusText.text = "在线";
                    m_StatusText.color = STATUS_ONLINE;
                    break;
                case "teaming":
                    m_StatusText.text = "组队中";
                    m_StatusText.color = STATUS_TEAMING;
                    break;
                case "ingame":
                    m_StatusText.text = "游戏中";
                    m_StatusText.color = STATUS_INGAME;
                    break;
                default:
                    m_StatusText.text = "离线";
                    m_StatusText.color = STATUS_OFFLINE;
                    break;
            }
        }

        private void SetPrimary(string label, Color32 color)
        {
            if (m_PrimaryBtn == null) return;
            m_PrimaryBtn.interactable = true;
            m_PrimaryBtn.image.color = color;
            SetButtonLabel(m_PrimaryBtn, label);
        }

        private void SetSecondary(string label, Color32 color, bool visible)
        {
            if (m_SecondaryBtn == null) return;
            m_SecondaryBtn.gameObject.SetActive(visible);
            m_SecondaryBtn.image.color = color;
            SetButtonLabel(m_SecondaryBtn, label);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        // ============================================================
        //  子控件构建（水平列表容器：LayoutElement 声明首选宽度，布局驱动排布）
        // ============================================================

        private Text CreateText(string name, string content, int fontSize, Vector2 size, Color color,
            TextAnchor alignment, float flexibleWidth = 0f)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            Text text = go.AddComponent<Text>();
            text.text = content;
            text.font = _builtinFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.flexibleWidth = flexibleWidth;
            return text;
        }

        private Button CreateButton(string name, string label, Color32 bgColor, Vector2 size, int fontSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(transform, false);
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;
            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.font = _builtinFont;
            btnText.fontSize = fontSize;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.raycastTarget = false;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            return button;
        }
    }
}
