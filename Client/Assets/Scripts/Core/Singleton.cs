using UnityEngine;

namespace DualEnigma.Core
{
    /// <summary>
    /// 泛型单例基类。子类继承 Singleton&lt;T&gt; 即可获得全局唯一实例，
    /// 自动 DontDestroyOnLoad 并销毁重复实例。
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T m_Instance;
        private static bool m_IsDestroyed;

        /// <summary>单例实例，不存在时自动创建 GameObject 并挂载</summary>
        public static T Instance
        {
            get
            {
                if (m_Instance == null && !m_IsDestroyed)
                {
                    m_Instance = FindObjectOfType<T>();
                    if (m_Instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        m_Instance = go.AddComponent<T>();
                    }
                }
                return m_Instance;
            }
        }

        public static bool HasInstance => m_Instance != null;

        protected virtual void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnSingletonInitialized();
            }
            else if (m_Instance != this)
            {
                // 场景中可能存在多个实例，只保留第一个
                Destroy(gameObject);
            }
        }

        /// <summary>单例初始化完成回调，子类重写以执行初始化逻辑（在 Awake 中调用）</summary>
        protected virtual void OnSingletonInitialized() { }

        protected virtual void OnDestroy()
        {
            if (m_Instance == this)
            {
                m_Instance = null;
                m_IsDestroyed = true;
            }
        }
    }
}
