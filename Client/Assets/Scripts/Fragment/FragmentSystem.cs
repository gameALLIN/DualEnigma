/// ============================================================
/// 文件名: FragmentSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 碎片系统管理器，管理碎片掉落、收集、存续和对象池。
/// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;

namespace DualEnigma.Fragment
{
    /// <summary>
    /// 碎片系统管理器。继承 Singleton<T>，注册 IFragmentSystem 到 ServiceLocator。
    /// 引用：碎片系统.md §3.1
    /// </summary>
    public class FragmentSystem : Singleton<FragmentSystem>, IFragmentSystem
    {
        /// <summary>同时接住判定窗口（秒）</summary>
        private const float SIMULTANEOUS_WINDOW = 0.1f;

        /// <summary>碎片预制体（待赋值）</summary>
        [SerializeField] private FragmentController _fragmentPrefab;

        /// <summary>碎片配置</summary>
        [SerializeField] private FragmentConfig _config;

        /// <summary>当前轮次（决定存续时间）</summary>
        private int _currentRound = 1;

        /// <summary>对象池父节点</summary>
        private Transform _poolRoot;

        /// <summary>碎片对象池</summary>
        private ObjectPool<FragmentController> _fragmentPool;

        /// <summary>活跃碎片字典</summary>
        private readonly Dictionary<int, FragmentController> _activeFragments = new Dictionary<int, FragmentController>();

        /// <summary>碎片ID自增计数器</summary>
        private int _nextFragmentId;

        /// <summary>收集记录（用于同时接住判定）</summary>
        private readonly Dictionary<int, CollectRecord> _collectRecords = new Dictionary<int, CollectRecord>();

        private struct CollectRecord
        {
            public byte playerId;
            public float timestamp;
        }

        /// <summary>当前场上存活的碎片数量</summary>
        public int ActiveCount => _activeFragments.Count;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IFragmentSystem>(this);

            _poolRoot = new GameObject("FragmentPoolRoot").transform;
            _poolRoot.SetParent(transform);

            if (_fragmentPrefab != null)
            {
                _fragmentPool = new ObjectPool<FragmentController>(_fragmentPrefab, 40, _poolRoot);
            }
            else
            {
                Debug.LogWarning("[FragmentSystem] 碎片预制体未赋值，对象池未初始化");
            }

            Debug.Log("[FragmentSystem] 碎片系统初始化完成");
        }

        /// <summary>设置当前轮次（影响存续时间）</summary>
        public void SetCurrentRound(int round)
        {
            _currentRound = round;
        }

        /// <summary>
        /// 生成碎片掉落计划（Host 调用）。
        /// </summary>
        public List<FragmentDropPlan> GenerateDropPlan(int disasterType, float densityFactor, uint seed)
        {
            List<FragmentDropPlan> plan = new List<FragmentDropPlan>();
            System.Random rng = new System.Random((int)seed);

            int totalBase = Mathf.RoundToInt(28 * densityFactor);
            int totalCount = Mathf.Max(totalBase, 10);

            for (int i = 0; i < totalCount; i++)
            {
                FragmentType type = GenerateFragmentType(rng);
                Vector2 pos = GenerateDropPosition(rng);

                plan.Add(new FragmentDropPlan
                {
                    FragmentId = _nextFragmentId++,
                    Type = type,
                    Position = pos,
                    DropTime = i < 5 ? i * 1f : 5f + (i - 5) * 0.5f,
                    Seed = (uint)rng.Next(),
                });
            }

            return plan;
        }

        /// <summary>
        /// 执行掉落计划（双方各自调用）。
        /// </summary>
        public void ExecuteDropPlan(List<FragmentDropPlan> plan)
        {
            StartCoroutine(ExecuteDropPlanCoroutine(plan));
        }

        private IEnumerator ExecuteDropPlanCoroutine(List<FragmentDropPlan> plan)
        {
            float elapsed = 0f;

            foreach (FragmentDropPlan item in plan)
            {
                while (elapsed < item.DropTime)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                SpawnFragment(item);
            }
        }

        /// <summary>
        /// 碎片被接住（由角色碰撞触发）。
        /// </summary>
        public void OnFragmentCollected(int fragmentId, byte playerId, bool isJumping)
        {
            if (!_activeFragments.TryGetValue(fragmentId, out FragmentController fragment))
                return;

            int multiplier = DetermineMultiplier(fragmentId, playerId, isJumping);

            EventBus.Instance.Publish(new FragmentCollectedEvent
            {
                fragmentId = fragmentId,
                playerId = playerId,
                isJumping = isJumping,
                multiplier = multiplier
            });

            fragment.SetState(FragmentState.Collected);
            ReleaseFragment(fragmentId);
        }

        /// <summary>
        /// 碎片自然消失。
        /// </summary>
        public void OnFragmentDespawned(int fragmentId)
        {
            ReleaseFragment(fragmentId);
        }

        private int DetermineMultiplier(int fragmentId, byte playerId, bool isJumping)
        {
            bool isSimultaneous = false;

            if (_collectRecords.TryGetValue(fragmentId, out CollectRecord record))
            {
                if (record.playerId != playerId && Time.time - record.timestamp < SIMULTANEOUS_WINDOW)
                {
                    isSimultaneous = true;
                }
            }

            _collectRecords[fragmentId] = new CollectRecord
            {
                playerId = playerId,
                timestamp = Time.time
            };

            if (isSimultaneous) return 3;
            if (isJumping) return 2;
            return 1;
        }

        private void SpawnFragment(FragmentDropPlan plan)
        {
            FragmentController fragment = null;

            if (_fragmentPool != null)
            {
                fragment = _fragmentPool.Get();
            }
            else
            {
                GameObject go = new GameObject($"Fragment_{plan.FragmentId}");
                go.AddComponent<BoxCollider2D>().isTrigger = true;
                fragment = go.AddComponent<FragmentController>();
            }

            float lifetime = _config != null ? _config.GetLifetime(_currentRound) : 3.0f;
            fragment.Initialize(plan, lifetime);
            _activeFragments[plan.FragmentId] = fragment;
        }

        private void ReleaseFragment(int fragmentId)
        {
            if (!_activeFragments.TryGetValue(fragmentId, out FragmentController fragment))
                return;

            _activeFragments.Remove(fragmentId);
            _collectRecords.Remove(fragmentId);

            if (_fragmentPool != null)
            {
                _fragmentPool.Release(fragment);
            }
            else if (fragment != null)
            {
                fragment.gameObject.SetActive(false);
            }
        }

        private FragmentType GenerateFragmentType(System.Random rng)
        {
            int roll = rng.Next(100);
            if (roll < 55) return FragmentType.IceCrystal;
            if (roll < 85) return FragmentType.Lava;
            return FragmentType.Rock;
        }

        private Vector2 GenerateDropPosition(System.Random rng)
        {
            float x = (float)(rng.NextDouble() * 20f - 10f);
            float y = (float)(rng.NextDouble() * 5f + 8f);
            return new Vector2(x, y);
        }
    }
}
