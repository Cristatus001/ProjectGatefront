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
    public sealed class EnemyWaveDirector : MonoBehaviour
    {
        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private TargetRegistry targetRegistry;
        [SerializeField] private WorldScroller worldScroller;
        [SerializeField] private SquadController squadController;
        [SerializeField] private PlayerConfig playerConfig;

        [Header("Development")]
        [SerializeField] private bool debugOverlayEnabled = true;

        private readonly List<EnemyWaveDefinition> orderedWaves =
            new List<EnemyWaveDefinition>();
        private readonly List<Vector3> spawnOffsets = new List<Vector3>(100);
        private int[] activeEnemiesByWave = Array.Empty<int>();
        private bool[] startedWaves = Array.Empty<bool>();
        private bool[] completedWaves = Array.Empty<bool>();
        private int nextWaveIndex;
        private bool resetting;
        private bool waveSchedulingEnabled = true;

        public event Action<int, EnemyWaveDefinition> WaveStarted;

        public event Action<int> WaveCompleted;

        public bool IsInitialized { get; private set; }

        public bool SimulationEnabled { get; private set; }

        public int WaveCount => orderedWaves.Count;

        public int StartedWaveCount => nextWaveIndex;

        public int CompletedWaveCount { get; private set; }

        public int ActiveEnemyCount => enemyPool == null
            ? 0
            : enemyPool.ActiveEnemyCount;

        public EnemyPool EnemyPool => enemyPool;

        public LevelConfig LevelConfig => levelConfig;

        private void Update()
        {
            if (!IsInitialized
                || !SimulationEnabled
                || !waveSchedulingEnabled
                || !worldScroller.ScrollingEnabled)
            {
                return;
            }

            while (nextWaveIndex < orderedWaves.Count
                && EnemyWaveMath.IsSpawnThresholdReached(
                    worldScroller.DistanceTravelled,
                    orderedWaves[nextWaveIndex].SpawnDistance))
            {
                SpawnWave(nextWaveIndex, orderedWaves[nextWaveIndex]);
                nextWaveIndex++;
            }
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
                || enemyPool == null
                || targetRegistry == null
                || worldScroller == null
                || squadController == null
                || playerConfig == null)
            {
                throw new InvalidOperationException(
                    "EnemyWaveDirector is missing required references.");
            }

            orderedWaves.Clear();
            for (int index = 0; index < levelConfig.EnemyWaves.Count; index++)
            {
                EnemyWaveDefinition wave = levelConfig.EnemyWaves[index];
                if (wave != null && wave.IsValid)
                {
                    orderedWaves.Add(wave);
                }
            }

            EnemyWaveMath.SortByDistance(orderedWaves);
            activeEnemiesByWave = new int[orderedWaves.Count];
            startedWaves = new bool[orderedWaves.Count];
            completedWaves = new bool[orderedWaves.Count];
            enemyPool.Initialize(
                targetRegistry,
                worldScroller,
                squadController,
                levelConfig.ActiveEnemyLimit);
            enemyPool.EnemyRemoved += HandleEnemyRemoved;
            IsInitialized = true;
            SetSimulationEnabled(true);
        }

        public void SetSimulationEnabled(bool enabled)
        {
            SimulationEnabled = enabled;
            if (enemyPool != null && enemyPool.IsInitialized)
            {
                enemyPool.SetSimulationEnabled(enabled);
            }
        }

        public void SetWaveSchedulingEnabled(bool enabled)
        {
            waveSchedulingEnabled = enabled;
        }

        public void ResetCombat()
        {
            if (!IsInitialized)
            {
                return;
            }

            resetting = true;
            enemyPool.ResetPool();
            resetting = false;
            Array.Clear(activeEnemiesByWave, 0, activeEnemiesByWave.Length);
            Array.Clear(startedWaves, 0, startedWaves.Length);
            Array.Clear(completedWaves, 0, completedWaves.Length);
            nextWaveIndex = 0;
            CompletedWaveCount = 0;
            waveSchedulingEnabled = true;
        }

        public void CopyOrderedWavesTo(List<EnemyWaveDefinition> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < orderedWaves.Count; index++)
            {
                destination.Add(orderedWaves[index]);
            }
        }

        private void SpawnWave(int waveIndex, EnemyWaveDefinition wave)
        {
            startedWaves[waveIndex] = true;
            WaveStarted?.Invoke(waveIndex + 1, wave);
            int spawnCount = EnemyWaveMath.ClampSpawnCount(
                wave.Count,
                enemyPool.ActiveEnemyCount,
                enemyPool.ActiveEnemyLimit);
            float usableTrackHalfWidth = Mathf.Max(
                0f,
                playerConfig.TrackHalfWidth - playerConfig.TrackBoundaryPadding);
            EnemySpawnPatternMath.GenerateOffsets(
                wave,
                usableTrackHalfWidth,
                wave.EnemyConfig.TargetRadius,
                spawnCount,
                spawnOffsets);

            int spawned = 0;
            float baseZ = squadController.CombatContactZ + wave.SpawnDepth;
            for (int index = 0; index < spawnOffsets.Count; index++)
            {
                Vector3 spawnPosition = spawnOffsets[index];
                spawnPosition.z += baseZ;
                if (enemyPool.Spawn(
                        wave.EnemyConfig,
                        spawnPosition,
                        waveIndex) != null)
                {
                    spawned++;
                }
            }

            activeEnemiesByWave[waveIndex] = spawned;
            if (spawned == 0)
            {
                CompleteWave(waveIndex);
            }
        }

        private void HandleEnemyRemoved(
            int waveIndex,
            EnemyReleaseReason _)
        {
            if (resetting
                || waveIndex < 0
                || waveIndex >= activeEnemiesByWave.Length
                || activeEnemiesByWave[waveIndex] <= 0)
            {
                return;
            }

            activeEnemiesByWave[waveIndex]--;
            if (activeEnemiesByWave[waveIndex] == 0
                && startedWaves[waveIndex])
            {
                CompleteWave(waveIndex);
            }
        }

        private void CompleteWave(int waveIndex)
        {
            if (completedWaves[waveIndex])
            {
                return;
            }

            completedWaves[waveIndex] = true;
            CompletedWaveCount++;
            WaveCompleted?.Invoke(waveIndex + 1);
        }

        private void OnDestroy()
        {
            if (enemyPool != null)
            {
                enemyPool.EnemyRemoved -= HandleEnemyRemoved;
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
                $"Enemies Alive: {ActiveEnemyCount}\nWave: "
                + $"{StartedWaveCount}/{WaveCount}  Distance: "
                + $"{worldScroller.DistanceTravelled:0}\nState: {state}");
        }
#endif
    }
}
