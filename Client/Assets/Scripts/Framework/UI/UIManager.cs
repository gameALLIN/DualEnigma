/// ============================================================
/// 文件名: UIManager.cs
/// 创建时间: 2026-07-10
/// 作者: DualEnigma
/// 描述: UI 总管理器，全局唯一 Canvas，栈式面板管理，ResMgr 加载预制体
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DualEnigma.Framework.Core;

namespace DualEnigma.Framework.UI
{
    /// <summary>
    /// UI 总管理器，负责全局 Canvas、4 层级子节点、栈式面板管理和面板缓存。
    /// 预制体通过 ResMgr 从 AssetPackage/Prefabs/UI/{面板名}/{面板名} 加载。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        // 预制体路径格式（相对于 AssetPackage/）
        private const string PREFAB_PATH_FORMAT = "Prefabs/UI/{0}/{0}";


        // 全局唯一 Canvas
        private Canvas m_RootCanvas;

        // 4 层级子节点（挂在全局 Canvas 下）
        private readonly Dictionary<UILayer, RectTransform> m_LayerRoots = new Dictionary<UILayer, RectTransform>();

        // 面板栈，记录当前打开的面板及其模式
        private readonly Stack<PanelStackEntry> m_PanelStack = new Stack<PanelStackEntry>();

        // 已实例化的面板缓存，避免重复加载
        private readonly Dictionary<string, UICtrlBase> m_PanelCache = new Dictionary<string, UICtrlBase>();

        // 所有层级，从底到顶
        private static readonly UILayer[] m_AllLayers =
        {
            UILayer.Bottom,
            UILayer.Normal,
            UILayer.Top,
            UILayer.Loading
        };

        protected override void OnSingletonInitialized()
        {
            LoadGlobalCanvas();
            if (m_RootCanvas != null)
            {
                CreateLayerRoots();
            }
        }

        /// <summary>从预制体加载全局 Canvas</summary>
        private void LoadGlobalCanvas()
        {
            GameObject canvasObj = GameObject.Find("MainCanvas");
            if (canvasObj == null)
            {
                Debug.LogError("[UIManager] 场景中未找到 MainCanvas，请确认 Canvas 预制体已放入场景");
                return;
            }

            m_RootCanvas = canvasObj.GetComponent<Canvas>();
            if (m_RootCanvas == null)
            {
                Debug.LogError("[UIManager] MainCanvas 上未找到 Canvas 组件");
            }
        }

        /// <summary>在全局 Canvas 下创建 4 个层级子节点</summary>
        private void CreateLayerRoots()
        {
            for (int i = 0; i < m_AllLayers.Length; i++)
            {
                UILayer layer = m_AllLayers[i];
                GameObject layerObj = new GameObject(layer.ToString());
                layerObj.transform.SetParent(m_RootCanvas.transform, false);

                RectTransform rt = layerObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                rt.SetSiblingIndex(i);

                m_LayerRoots[layer] = rt;
            }
        }

        /// <summary>获取指定层级根节点（常驻悬浮层等非面板栈 UI 挂载用）</summary>
        public RectTransform GetLayerRoot(UILayer layer)
        {
            return m_LayerRoots.TryGetValue(layer, out RectTransform root) ? root : null;
        }

        /// <summary>将 UIMode 映射到对应的 UILayer</summary>
        private static UILayer GetLayerByMode(UIMode mode)
        {
            switch (mode)
            {
                case UIMode.HUD: return UILayer.Bottom;
                case UIMode.FullScreen: return UILayer.Normal;
                case UIMode.Popup: return UILayer.Top;
                default: return UILayer.Normal;
            }
        }

        /// <summary>从 Controller 类型名提取面板名（去掉 Ctrl 后缀）</summary>
        private static string GetPanelName(System.Type type)
        {
            string name = type.Name;
            if (name.EndsWith("Ctrl"))
            {
                name = name.Substring(0, name.Length - 4);
            }
            return name;
        }

        /// <summary>打开面板。全屏面板会隐藏当前栈顶，已缓存面板直接复用</summary>
        public void Push<T>(UIMode mode = UIMode.FullScreen) where T : UICtrlBase
        {
            string panelName = GetPanelName(typeof(T));

            // 尝试从缓存复用
            if (m_PanelCache.TryGetValue(panelName, out UICtrlBase panel))
            {
                // FullScreen 模式独占显示，需先隐藏当前栈顶
                if (mode == UIMode.FullScreen && m_PanelStack.Count > 0)
                {
                    PanelStackEntry currentTop = m_PanelStack.Peek();
                    ((IUIPanel)currentTop.Panel).OnHide();
                    currentTop.Panel.gameObject.SetActive(false);
                }

                panel.gameObject.SetActive(true);
                panel.transform.SetAsLastSibling();
                ((IUIPanel)panel).OnShow();
                m_PanelStack.Push(new PanelStackEntry { Panel = panel, Mode = mode });
                return;
            }

            // 通过 ResMgr 加载预制体
            string prefabPath = string.Format(PREFAB_PATH_FORMAT, panelName);
            GameObject prefab = ResMgr.Instance.LoadPrefab(prefabPath);
            if (prefab == null)
            {
                return;
            }

            // FullScreen 模式独占显示，需先隐藏当前栈顶
            if (mode == UIMode.FullScreen && m_PanelStack.Count > 0)
            {
                PanelStackEntry currentTop = m_PanelStack.Peek();
                ((IUIPanel)currentTop.Panel).OnHide();
                currentTop.Panel.gameObject.SetActive(false);
            }

            // 实例化到对应层级下
            UILayer layer = GetLayerByMode(mode);
            Transform parent = m_LayerRoots[layer];
            GameObject panelObj = Instantiate(prefab, parent, false);
            panelObj.name = panelName;

            // 获取 Controller 组件
            panel = panelObj.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] 预制体上未找到组件: {typeof(T).Name}");
                Destroy(panelObj);
                return;
            }

            m_PanelCache[panelName] = panel;

            // 生命周期回调
            ((IUIPanel)panel).OnCreate();
            ((IUIPanel)panel).OnShow();

            m_PanelStack.Push(new PanelStackEntry { Panel = panel, Mode = mode });
        }

        /// <summary>关闭栈顶面板。若弹出的是全屏面板，恢复显示下方面板</summary>
        public void Pop()
        {
            if (m_PanelStack.Count == 0)
            {
                Debug.LogWarning("[UIManager] 面板栈为空，无法 Pop");
                return;
            }

            PanelStackEntry entry = m_PanelStack.Pop();
            ((IUIPanel)entry.Panel).OnHide();
            entry.Panel.gameObject.SetActive(false);

            if (entry.Mode == UIMode.FullScreen && m_PanelStack.Count > 0)
            {
                PanelStackEntry newTop = m_PanelStack.Peek();
                if (!newTop.Panel.gameObject.activeSelf)
                {
                    newTop.Panel.gameObject.SetActive(true);
                    ((IUIPanel)newTop.Panel).OnShow();
                }
            }
        }

        /// <summary>回退到指定面板，中间面板全部弹出并隐藏</summary>
        public void PopTo<T>() where T : UICtrlBase
        {
            string targetName = GetPanelName(typeof(T));

            while (m_PanelStack.Count > 0)
            {
                PanelStackEntry top = m_PanelStack.Peek();
                string topName = GetPanelName(top.Panel.GetType());

                if (topName == targetName)
                {
                    if (!top.Panel.gameObject.activeSelf)
                    {
                        top.Panel.gameObject.SetActive(true);
                        ((IUIPanel)top.Panel).OnShow();
                    }
                    return;
                }

                m_PanelStack.Pop();
                ((IUIPanel)top.Panel).OnHide();
                top.Panel.gameObject.SetActive(false);
            }

            Debug.LogWarning("[UIManager] 栈中未找到面板: " + targetName);
        }

        /// <summary>获取当前栈顶面板</summary>
        public UICtrlBase GetTopPanel()
        {
            if (m_PanelStack.Count == 0)
            {
                return null;
            }
            return m_PanelStack.Peek().Panel;
        }

        /// <summary>
        /// 批量设置当前面板栈内全部面板的可见性（不触发 OnShow/OnHide 生命周期）。
        /// 用于对局开始时整体隐藏主界面、对局结束后整体恢复——面板栈结构与状态保持不变。
        /// </summary>
        public void SetPanelsVisible(bool visible)
        {
            foreach (PanelStackEntry entry in m_PanelStack)
            {
                if (entry.Panel != null && entry.Panel.gameObject.activeSelf != visible)
                    entry.Panel.gameObject.SetActive(visible);
            }
        }

        /// <summary>面板栈条目</summary>
        private struct PanelStackEntry
        {
            public UICtrlBase Panel;
            public UIMode Mode;
        }
    }
}
