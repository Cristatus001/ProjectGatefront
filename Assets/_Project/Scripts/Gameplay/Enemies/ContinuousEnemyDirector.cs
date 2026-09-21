using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class ContinuousEnemyDirector : MonoBehaviour
    {
        private const int MaximumGroupsPerFrame = 8;

        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private TargetRegistry targetRegistry;
        [SerializeField] private WorldScroller worldScroller;
        [SerializeField] private SquadController squadController;
        [SerializeField] private PlayerConfig playerConfig;

        [Header("Development")]
        [SerializeField] private bool debugOverlayEnabled = true;

        private readonly List<CombatSectionDefinition> orderedSections =
            new List<CombatSectionDefinition>();
        private readonly List<Vector3> spawnOffsets = new List<Vector3>(100);
        private float nextSpawnDistance;
        private int scheduledGroupCount;
        private bool spawnSchedulingEnabled = true;
        private float spawnIntervalMultiplier = 1f;

        public event Action<int, int> GroupSpawned;
        public event Action BossSpawnRequested;

        public bool IsInitialized { get; private set; }
        public bool SimulationEnabled { get; private set; }
        public bool BossTriggerReached { get; private set; }
        public int SectionCount => orderedSections.Count;
        public int ScheduledGroupCount => scheduledGroupCount;
        public int CurrentSectionNumber { get; private set; }
        public float NextSpawnDistance => nextSpawnDistance;
        public float SpawnIntervalMultiplier => spawnIntervalMultiplier;
        public int ActiveEnemyCount => enemyPool == null
            ? 0
            : enemyPool.ActiveEnemyCount;
        public EnemyPool EnemyPool => enemyPool;
        public LevelConfig LevelConfig => levelConfig;

        private void Update()
        {
            Tick();
        }

        public void Configure(
            LevelConfig config,
            EnemyPool pool,
            TargetRegistry registry,
            WorldScroller scroller,
            SquadController squad,
            PlayerConfig trackConfig)
        {
            levelConfig = config;
            enemyPool = pool;
            targetRegistry = registry;
            worldScroller = scroller;
            squadController = squad;
            playerConfig = trackConfig;
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (levelConfig == null
                || levelConfig.NormalEnemy == null
                || enemyPool == null
                || targetRegistry == null
                || worldScroller == null
                || squadController == null
                || playerConfig == null)
            {
                throw new InvalidOperationException(
                    "ContinuousEnemyDirector is missing required references.");
            }

            orderedSections.Clear();
            for (int index = 0; index < levelConfig.CombatSections.Count; index++)
            {
                CombatSectionDefinition section = levelConfig.CombatSections[index];
                if (section != null && section.IsValid)
                {
                    orderedSections.Add(section);
                }
            }

            ContinuousSpawnMath.SortByStartDistance(orderedSections);
            enemyPool.Initialize(
                targetRegistry,
                worldScroller,
                squadController,
                levelConfig.ActiveEnemyLimit);
            IsInitialized = true;
            ResetCombat();
            SetSimulationEnabled(true);
        }

        public void Tick()
        {
            if (!IsInitialized
                || !SimulationEnabled
                || !worldScroller.ScrollingEnabled)
            {
                return;
            }

            if (!BossTriggerReached
                && worldScroller.DistanceTravelled >= levelConfig.BossDistance)
            {
                BossTriggerReached = true;
                BossSpawnRequested?.Invoke();
            }

            if (!spawnSchedulingEnabled)
            {
                return;
            }

            int groupsProcessed = 0;
            while (groupsProcessed < MaximumGroupsPerFrame
                && worldScroller.DistanceTravelled >= nextSpawnDistance)
            {
                int sectionIndex = ContinuousSpawnMath.FindSection(
                    orderedSections,
                    nextSpawnDistance);
                if (sectionIndex < 0)
                {
                    float nextStart = float.PositiveInfinity;
                    for (int index = 0; index < orderedSections.Count; index++)
                    {
                        float start = orderedSections[index].StartDistance;
                        if (start > nextSpawnDistance && start < nextStart)
                        {
                            nextStart = start;
                        }
                    }

                    nextSpawnDistance = nextStart;
                    if (float.IsPositiveInfinity(nextStart))
                    {
                        return;
                    }

                    continue;
                }

                CombatSectionDefinition section = orderedSections[sectionIndex];
                CurrentSectionNumber = sectionIndex + 1;
                int requested = ContinuousSpawnMath.GroupSize(
                    section,
                    scheduledGroupCount);
                int spawnCount = ContinuousSpawnMath.ClampToActiveLimit(
                    requested,
                    enemyPool.ActiveEnemyCount,
                    enemyPool.ActiveEnemyLimit);
                SpawnGroup(section, spawnCount);
                scheduledGroupCount++;
                nextSpawnDistance = ContinuousSpawnMath.NextSpawnDistance(
                    nextSpawnDistance,
                    worldScroller.CurrentScrollSpeed * spawnIntervalMultiplier,
                    section);
                groupsProcessed++;
            }
        }

        public void SetSimulationEnabled(bool enabled)
        {
            SimulationEnabled = enabled;
            if (enemyPool != null && enemyPool.IsInitialized)
            {
                enemyPool.SetSimulationEnabled(enabled);
            }
        }

        public void SetSpawnSchedulingEnabled(bool enabled)
        {
            spawnSchedulingEnabled = enabled;
        }

        public void SetSpawnIntervalMultiplier(float multiplier)
        {
            spawnIntervalMultiplier = float.IsNaN(multiplier)
                || float.IsInfinity(multiplier)
                ? 1f
                : Mathf.Clamp(multiplier, 1f, 10f);
        }

        public void ResetCombat()
        {
            if (!IsInitialized)
            {
                return;
            }

            enemyPool.ResetPool();
            scheduledGroupCount = 0;
            CurrentSectionNumber = 0;
            BossTriggerReached = false;
            spawnSchedulingEnabled = true;
            spawnIntervalMultiplier = 1f;
            nextSpawnDistance = orderedSections.Count > 0
                ? orderedSections[0].StartDistance
                : float.PositiveInfinity;
        }

        public void CopyOrderedSectionsTo(List<CombatSectionDefinition> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            destination.AddRange(orderedSections);
        }

        private void SpawnGroup(CombatSectionDefinition section, int count)
        {
            if (count <= 0)
            {
                return;
            }

            EnemyConfig normalEnemy = levelConfig.NormalEnemy;
            float roadHalfWidth = Mathf.Max(
                0f,
                playerConfig.TrackHalfWidth - playerConfig.TrackBoundaryPadding);
            EnemySpawnPatternMath.GenerateOffsets(
                section,
                roadHalfWidth,
                normalEnemy.TargetRadius,
                count,
                spawnOffsets);

            int spawned = 0;
            float baseZ = squadController.CombatContactZ + section.SpawnDepth;
            for (int index = 0; index < spawnOffsets.Count; index++)
            {
                Vector3 position = spawnOffsets[index];
                position.z += baseZ;
                if (enemyPool.Spawn(normalEnemy, position, scheduledGroupCount) != null)
                {
                    spawned++;
                }
            }

            if (spawned > 0)
            {
                GroupSpawned?.Invoke(scheduledGroupCount + 1, spawned);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!debugOverlayEnabled || !IsInitialized)
            {
                return;
            }

            string state = squadController.IsDefeated ? "Defeated" : "Fighting";
            GUI.Box(
                new Rect(12f, 186f, 310f, 86f),
                $"Enemies Alive: {ActiveEnemyCount}\nSection: "
                + $"{CurrentSectionNumber}/{SectionCount}  Distance: "
                + $"{worldScroller.DistanceTravelled:0}\nState: {state}");
        }
#endif
    }
}
