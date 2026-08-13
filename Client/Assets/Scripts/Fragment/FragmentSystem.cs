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
using DualEnigma.Framework.Core;
using DualEnigma.Data;
using DualEnigma.Skill;
using DualEnigma.Building;

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

        private float WarmBrickWindow => _config != null ? _config.WarmBrickWindow : 0.1f;
        private float PassiveTriggerRadius => _config != null ? _config.PassiveTriggerRadius : 3f;

        /// <summary>被动技能触发概率（0-1）</summary>
        [SerializeField] private float _passiveTriggerChance = 0.3f;

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

        /// <summary>已收集碎片的类型记录（碎片被收集后仍可查询类型）</summary>
        private readonly Dictionary<int, FragmentType> _collectedFragmentTypes = new Dictionary<int, FragmentType>();

        /// <summary>碎片被点燃的时间戳记录（用于温砖转换判定）</summary>
        private readonly Dictionary<int, float> _ignitedTimestamps = new Dictionary<int, float>();

        /// <summary>碎片ID自增计数器</summary>
        private int _nextFragmentId;

        /// <summary>OnUpdate 超时碎片ID缓存（复用避免每帧分配）</summary>
        private readonly List<int> _timedOutCache = new List<int>();

        /// <summary>GetActiveFragments 返回的缓存列表（复用避免每帧分配）</summary>
        private readonly List<FragmentController> _activeFragmentsCache = new List<FragmentController>();

        /// <summary>收集记录（用于同时接住判定）</summary>
        private readonly Dictionary<int, CollectRecord> _collectRecords = new Dictionary<int, CollectRecord>();

        private struct CollectRecord
        {
            public byte playerId;
            public float timestamp;
        }

        /// <summary>待确认收集记录（第一个玩家接住后暂存，等待同时接住判定窗口）</summary>
        private readonly Dictionary<int, PendingCollect> _pendingCollects = new Dictionary<int, PendingCollect>();

        private struct PendingCollect
        {
            public byte playerId;
            public float timestamp;
            public bool isJumping;
            public FragmentController fragment;
            public FragmentType type;
        }

        /// <summary>当前场上存活的碎片数量</summary>
        public int ActiveCount => _activeFragments.Count;

        /// <summary>
        /// 获取当前场上所有活跃碎片的列表。
        /// 供技能系统（冻结效果等）遍历碎片使用，避免 FindObjectsOfType 调用。
        /// 返回缓存的 List 引用，调用方不应修改返回的 List。
        /// </summary>
        /// <returns>活跃碎片列表（缓存引用，只读使用）</returns>
        public List<FragmentController> GetActiveFragments()
        {
            _activeFragmentsCache.Clear();
            foreach (var kvp in _activeFragments)
            {
                if (kvp.Value != null)
                    _activeFragmentsCache.Add(kvp.Value);
            }
            return _activeFragmentsCache;
        }

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


            // 尝试通过 DataManager 加载碎片配置，失败则回退到 Inspector 手动赋值的 SerializeField

            FragmentConfig dmConfig = DataManager.Instance.LoadConfig<FragmentConfig>("FragmentConfig");

            if (dmConfig != null)

            {

                _config = dmConfig;

            }

            else if (_config != null)

            {

                Debug.LogWarning("[FragmentSystem] DataManager 加载 FragmentConfig 失败，回退到 Inspector 手动赋值");

            }


            // 订阅碎片消失事件（由 FragmentController 通过 EventBus 发布）

            if (EventBus.HasInstance)
                EventBus.Instance.Subscribe<FragmentDespawnedEvent>(OnFragmentDespawnedEvent);


            Debug.Log("[FragmentSystem] 碎片系统初始化完成");

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<FragmentDespawnedEvent>(OnFragmentDespawnedEvent);
        }

        /// <summary>设置当前轮次（影响存续时间）</summary>
        public void SetCurrentRound(int round)
        {
            _currentRound = round;
        }

        /// <summary>
        /// 生成碎片掉落计划（Host 调用）。
        /// 分预告阶段和收集阶段：预告阶段少量碎片1s间隔，收集阶段大量碎片0.5s间隔。
        /// </summary>
        public List<FragmentDropPlan> GenerateDropPlan(int disasterType, float densityFactor, uint seed)
        {
            List<FragmentDropPlan> plan = new List<FragmentDropPlan>();
            System.Random rng = new System.Random((int)seed);

            int previewCount = _config != null ? _config.PreviewCount : 5;
            int collectCount = _config != null ? _config.CollectPhaseCount : 25;

            collectCount = Mathf.RoundToInt(collectCount * densityFactor);
            collectCount = Mathf.Max(collectCount, 10);

            int disasterCategory = disasterType / 100;

            for (int i = 0; i < previewCount; i++)
            {
                plan.Add(new FragmentDropPlan
                {
                    FragmentId = _nextFragmentId++,
                    Type = GenerateFragmentType(rng, disasterCategory),
                    Position = GenerateDropPosition(rng),
                    DropTime = i * 1f,
                    Seed = (uint)rng.Next(),
                });
            }

            for (int i = 0; i < collectCount; i++)
            {
                plan.Add(new FragmentDropPlan
                {
                    FragmentId = _nextFragmentId++,
                    Type = GenerateFragmentType(rng, disasterCategory),
                    Position = GenerateDropPosition(rng),
                    DropTime = previewCount + i * 0.5f,
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
        /// 第一个玩家接住时暂存为待确认状态，不立即释放碎片；
        /// 100ms 窗口内第二玩家接住则触发×3倍率，否则超时后正常完成收集。
        /// </summary>
        public void OnFragmentCollected(int fragmentId, byte playerId, bool isJumping)
        {
            if (!_activeFragments.TryGetValue(fragmentId, out FragmentController fragment))
                return;

            // 检查是否有待确认的收集记录（同时接住判定）
            if (_pendingCollects.TryGetValue(fragmentId, out PendingCollect pending))
            {
                // 同一玩家重复触发，忽略
                if (pending.playerId == playerId)
                    return;

                // 不同玩家在窗口内接住 → 同时接住 ×3
                _pendingCollects.Remove(fragmentId);

                int multiplier = DetermineMultiplier(fragmentId, playerId, isJumping);

                // 为第一个玩家发布事件（倍率升级为×3）
                EventBus.Instance.Publish(new FragmentCollectedEvent
                {
                    fragmentId = fragmentId,
                    playerId = pending.playerId,
                    isJumping = pending.isJumping,
                    multiplier = multiplier
                });

                // 为第二个玩家发布事件
                EventBus.Instance.Publish(new FragmentCollectedEvent
                {
                    fragmentId = fragmentId,
                    playerId = playerId,
                    isJumping = isJumping,
                    multiplier = multiplier
                });

                // 被动技能检查（双方均触发，同时接住概率100%）
                CheckPassiveSkills(pending.playerId, pending.type, pending.fragment.transform.position, pending.isJumping, true);
                CheckPassiveSkills(playerId, fragment.Type, fragment.transform.position, isJumping, true);

                fragment.SetState(FragmentState.Collected);
                _collectedFragmentTypes[fragmentId] = fragment.Type;
                ReleaseFragment(fragmentId);
                return;
            }

            // 第一个玩家接住 → 存入待确认记录，不立即释放碎片
            // 设置为 Collected 状态防止 FragmentController.Update 继续倒计时导致消失
            fragment.SetState(FragmentState.Collected);

            // 存入收集记录供 DetermineMultiplier 判定
            _collectRecords[fragmentId] = new CollectRecord
            {
                playerId = playerId,
                timestamp = Time.time
            };

            _pendingCollects[fragmentId] = new PendingCollect
            {
                playerId = playerId,
                timestamp = Time.time,
                isJumping = isJumping,
                fragment = fragment,
                type = fragment.Type
            };
        }

        /// <summary>
        /// 每帧更新，处理待确认收集记录的超时。
        /// 超过 SIMULTANEOUS_WINDOW 窗口的记录按正常收集（×1或×2）完成并释放碎片。
        /// </summary>
        private void Update()
        {
            OnUpdate(Time.deltaTime);
        }

        private void OnUpdate(float deltaTime)
        {
            if (_pendingCollects.Count == 0)
                return;

            _timedOutCache.Clear();
            foreach (var kvp in _pendingCollects)
            {
                if (Time.time - kvp.Value.timestamp >= SIMULTANEOUS_WINDOW)
                {
                    _timedOutCache.Add(kvp.Key);
                }
            }

            foreach (int fragmentId in _timedOutCache)
            {
                CompletePendingCollect(fragmentId);
            }
        }

        /// <summary>
        /// 完成超时的待确认收集（窗口内无第二玩家接住，按正常倍率完成）。
        /// </summary>
        private void CompletePendingCollect(int fragmentId)
        {
            if (!_pendingCollects.TryGetValue(fragmentId, out PendingCollect pending))
                return;

            _pendingCollects.Remove(fragmentId);

            // 窗口超时，无同时接住 → 根据是否跳跃决定倍率
            int multiplier = pending.isJumping ? 2 : 1;

            EventBus.Instance.Publish(new FragmentCollectedEvent
            {
                fragmentId = fragmentId,
                playerId = pending.playerId,
                isJumping = pending.isJumping,
                multiplier = multiplier
            });

            // 被动技能检查（地面接住概率30%，跳跃接住概率50%）
            CheckPassiveSkills(pending.playerId, pending.type, pending.fragment.transform.position, pending.isJumping, false);

            if (pending.fragment != null)
            {
                pending.fragment.SetState(FragmentState.Collected);
            }
            _collectedFragmentTypes[fragmentId] = pending.type;
            ReleaseFragment(fragmentId);
        }

        /// <summary>
        /// 碎片自然消失事件处理（由 FragmentController 通过 EventBus 发布）。
        /// </summary>
        private void OnFragmentDespawnedEvent(FragmentDespawnedEvent evt)
        {
            OnFragmentDespawned(evt.fragmentId);
        }

        /// <summary>
        /// 碎片自然消失。
        /// </summary>
        public void OnFragmentDespawned(int fragmentId)
        {
            ReleaseFragment(fragmentId);
        }

        /// <summary>
        /// 查询碎片类型（包括已收集但未消耗的碎片）。
        /// 引用：合成系统.md §4.2 碎片验证消耗
        /// </summary>
        public bool TryGetFragmentType(int fragmentId, out FragmentType type)
        {
            if (_collectedFragmentTypes.TryGetValue(fragmentId, out type))
                return true;

            if (_activeFragments.TryGetValue(fragmentId, out FragmentController fragment))
            {
                type = fragment.Type;
                return true;
            }

            type = default;
            return false;
        }

        // ──────────────────────────────────────────────
        //  被动技能触发
        // ──────────────────────────────────────────────

        /// <summary>
        /// 检查收集者被动技能并触发效果。
        /// - FrostAura（寒霜体质）：收集冰晶碎片时有概率冻结周围碎片
        /// - FlameAura（烈焰体质）：收集熔岩碎片时有概率点燃周围碎片
        /// 引用：技能系统.md §4.2 被动技能触发时机
        /// </summary>
        private void CheckPassiveSkills(byte playerId, FragmentType collectedType, Vector2 position, bool isJumping, bool isSimultaneous)
        {
            ISkillSystem skillSys = ServiceLocator.Get<ISkillSystem>();
            if (skillSys == null)
                return;

            float triggerChance;
            if (isSimultaneous)
                triggerChance = 1.0f;
            else if (isJumping)
                triggerChance = 0.5f;
            else
                triggerChance = _passiveTriggerChance;

            float passiveBonus = skillSys.GetPassiveChanceBonus(playerId);
            triggerChance += passiveBonus;
            triggerChance = Mathf.Clamp01(triggerChance);

            if (collectedType == FragmentType.IceCrystal
                && skillSys.IsPassiveActive(playerId, PassiveSkillType.FrostAura)
                && Random.Range(0f, 1f) <= triggerChance)
            {
                FreezeNearbyFragments(position, PassiveTriggerRadius);
                Debug.Log($"[FragmentSystem] 玩家{playerId} 寒霜体质触发 (概率{triggerChance:F2})");
            }

            if (collectedType == FragmentType.Lava
                && skillSys.IsPassiveActive(playerId, PassiveSkillType.FlameAura)
                && Random.Range(0f, 1f) <= triggerChance)
            {
                IgniteNearbyFragments(position, PassiveTriggerRadius);
                Debug.Log($"[FragmentSystem] 玩家{playerId} 烈焰体质触发 (概率{triggerChance:F2})");
            }
        }

        /// <summary>
        /// 点燃范围内的 Falling 状态碎片。
        /// </summary>
        private void IgniteNearbyFragments(Vector2 center, float radius)
        {
            foreach (var kvp in _activeFragments)
            {
                FragmentController fragment = kvp.Value;
                if (fragment == null || fragment.State != FragmentState.Falling)
                    continue;

                if (Vector2.Distance(fragment.transform.position, center) <= radius)
                {
                    fragment.SetIgnited();
                }
            }
        }

        /// <summary>
        /// 冻结范围内的 Falling 或 Ignited 状态碎片。
        /// 若碎片此前被点燃且在温砖窗口内，将触发温砖转换。
        /// </summary>
        private void FreezeNearbyFragments(Vector2 center, float radius)
        {
            foreach (var kvp in _activeFragments)
            {
                FragmentController fragment = kvp.Value;
                if (fragment == null)
                    continue;

                // 仅 Falling 或 Ignited 状态碎片可被冻结
                if (fragment.State != FragmentState.Falling
                    && fragment.State != FragmentState.Ignited)
                    continue;

                if (Vector2.Distance(fragment.transform.position, center) <= radius)
                {
                    fragment.SetFrozen();
                }
            }
        }

        // ──────────────────────────────────────────────
        //  温砖触发逻辑
        // ──────────────────────────────────────────────

        /// <summary>
        /// 碎片被点燃回调（由 FragmentController.SetIgnited 调用）。
        /// 记录点燃时间戳，用于后续温砖转换判定。
        /// </summary>
        public void OnFragmentIgnited(int fragmentId)
        {
            _ignitedTimestamps[fragmentId] = Time.time;
            Debug.Log($"[FragmentSystem] 碎片{fragmentId}被点燃");
        }

        /// <summary>
        /// 碎片被冻结回调（由 FragmentController.SetFrozen 调用）。
        /// 若碎片在温砖窗口内被点燃后冻结，则转化为温砖。
        /// 引用：碎片系统.md §4.4 温砖触发
        /// </summary>
        public void OnFragmentFrozen(int fragmentId)
        {
            Debug.Log($"[FragmentSystem] 碎片{fragmentId}被冻结");

            // 检查温砖转换：点燃后 100ms 内冻结
            if (_ignitedTimestamps.TryGetValue(fragmentId, out float ignitedTime))
            {
                _ignitedTimestamps.Remove(fragmentId);

                if (Time.time - ignitedTime <= WarmBrickWindow)
                {
                    ConvertToWarmBrick(fragmentId);
                    return;
                }
            }
        }

        /// <summary>
        /// 将碎片转化为温砖。
        /// 设置状态为 ConvertedToWarmBrick 并发布事件通知其他系统。
        /// 温砖作为特殊材料，角色拾取后可直接用于建造。
        /// </summary>
        private void ConvertToWarmBrick(int fragmentId)
        {
            if (!_activeFragments.TryGetValue(fragmentId, out FragmentController fragment))
                return;

            Vector2 position = fragment.transform.position;
            fragment.SetState(FragmentState.ConvertedToWarmBrick);

            EventBus.Instance.Publish(new FragmentWarmBrickConvertedEvent
            {
                fragmentId = fragmentId,
                position = position
            });

            Debug.Log($"[FragmentSystem] 碎片{fragmentId}转化为温砖 @ {position}");
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
                fragment.ResetState();
            }
            else
            {
                GameObject go = new GameObject($"Fragment_{plan.FragmentId}");
                go.AddComponent<BoxCollider2D>().isTrigger = true;
                go.AddComponent<Rigidbody2D>();
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
            _ignitedTimestamps.Remove(fragmentId);
            _pendingCollects.Remove(fragmentId);

            if (_fragmentPool != null)
            {
                _fragmentPool.Release(fragment);
            }
            else if (fragment != null)
            {
                fragment.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 清除所有碎片状态并清空对象池（新局开始或系统重置时调用）。
        /// </summary>
        public void ClearPool()
        {
            foreach (var kvp in _activeFragments)
            {
                if (kvp.Value != null)
                {
                    if (_fragmentPool != null)
                        _fragmentPool.Release(kvp.Value);
                    else
                        Destroy(kvp.Value.gameObject);
                }
            }

            _activeFragments.Clear();
            _collectedFragmentTypes.Clear();
            _ignitedTimestamps.Clear();
            _pendingCollects.Clear();
            _collectRecords.Clear();
            _timedOutCache.Clear();
            _activeFragmentsCache.Clear();

            if (_fragmentPool != null)
                _fragmentPool.Clear();
        }

        private FragmentType GenerateFragmentType(System.Random rng, int disasterCategory)
        {
            float roll = (float)rng.NextDouble();

            float iceProb;
            float lavaProb;

            if (_config != null)
            {
                iceProb = _config.GetTypeProbability(FragmentType.IceCrystal);
                lavaProb = _config.GetTypeProbability(FragmentType.Lava);
            }
            else
            {
                iceProb = 0.55f;
                lavaProb = 0.30f;
            }

            switch (disasterCategory)
            {
                case 0:
                    iceProb += 0.05f;
                    lavaProb += 0.05f;
                    break;
                case 1:
                    break;
                case 3:
                    iceProb -= 0.05f;
                    lavaProb -= 0.05f;
                    break;
            }

            if (roll < iceProb) return FragmentType.IceCrystal;
            if (roll < iceProb + lavaProb) return FragmentType.Lava;
            return FragmentType.Rock;
        }

        private Vector2 GenerateDropPosition(System.Random rng)
        {
            float minX, maxX;
            const float minY = 8f;
            const float maxY = 13f;

            if (_config != null)
            {
                minX = _config.DropRangeMin;
                maxX = _config.DropRangeMax;
            }
            else
            {
                minX = -10f;
                maxX = 10f;
            }

            IBuildSystem buildSys = ServiceLocator.Get<IBuildSystem>();
            if (buildSys != null && buildSys.Buildings.Count > 0)
            {
                float buildingCenterX = 0f;
                int count = 0;
                foreach (var b in buildSys.Buildings)
                {
                    buildingCenterX += b.GridPosition.x;
                    count++;
                }
                if (count > 0)
                    buildingCenterX /= count;

                float halfRange = (maxX - minX) * 0.5f;
                minX = buildingCenterX - halfRange;
                maxX = buildingCenterX + halfRange;
            }

            float x = (float)(rng.NextDouble() * (maxX - minX) + minX);
            float y = (float)(rng.NextDouble() * (maxY - minY) + minY);
            return new Vector2(x, y);
        }
    }
}
