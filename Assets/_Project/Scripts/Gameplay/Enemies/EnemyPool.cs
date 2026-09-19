using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private Transform enemyContainer;
        [SerializeField, Range(1, 100)] private int prewarmCapacity = 100;
        [SerializeField, Min(0f)] private float contactPadding = 0.12f;
        [SerializeField, Min(0.5f)] private float passBehindDistance = 4f;

        private readonly Stack<EnemyController> inactiveEnemies =
            new Stack<EnemyController>(100);
        private readonly List<EnemyController> activeEnemies =
            new List<EnemyController>(100);
        private TargetRegistry targetRegistry;
        private WorldScroller worldScroller;
        private SquadController squadController;
        private int spawnSequence;
        private int activeEnemyLimit = 100;

        public event Action<EnemyController> EnemySpawned;

        public event Action<EnemyController> EnemyKilled;

        public event Action<EnemyController, int> EnemyReachedSquad;

        public event Action<int, EnemyReleaseReason> EnemyRemoved;

        public bool IsInitialized { get; private set; }

        public bool SimulationEnabled { get; private set; }

        public int ActiveEnemyCount => activeEnemies.Count;

        public int InactiveEnemyCount => inactiveEnemies.Count;

        public int TotalCreated { get; private set; }

        public int ActiveEnemyLimit => activeEnemyLimit;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(
            EnemyController prefab,
            Transform container,
            int desiredPrewarmCapacity,
            float horizontalContactPadding,
            float despawnBehindDistance)
        {
            enemyPrefab = prefab;
            enemyContainer = container;
            prewarmCapacity = Mathf.Clamp(desiredPrewarmCapacity, 1, 100);
            contactPadding = Mathf.Max(0f, horizontalContactPadding);
            passBehindDistance = Mathf.Max(0.5f, despawnBehindDistance);
        }

        public void Initialize(
            TargetRegistry registry,
            WorldScroller scroller,
            SquadController squad,
            int maximumActiveEnemies)
        {
            if (IsInitialized)
            {
                return;
            }

            targetRegistry = registry != null
                ? registry
                : throw new ArgumentNullException(nameof(registry));
            worldScroller = scroller != null
                ? scroller
                : throw new ArgumentNullException(nameof(scroller));
            squadController = squad != null
                ? squad
                : throw new ArgumentNullException(nameof(squad));
            if (enemyPrefab == null)
            {
                throw new InvalidOperationException(
                    "EnemyPool requires an enemy prefab.");
            }

            if (enemyContainer == null)
            {
                enemyContainer = transform;
            }

            activeEnemyLimit = Mathf.Clamp(maximumActiveEnemies, 1, 200);
            int safePrewarm = Mathf.Min(prewarmCapacity, activeEnemyLimit);
            for (int index = 0; index < safePrewarm; index++)
            {
                inactiveEnemies.Push(CreateEnemy());
            }

            SimulationEnabled = true;
            IsInitialized = true;
        }

        public EnemyController Spawn(
            EnemyConfig config,
            Vector3 worldPosition,
            int waveIndex)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "EnemyPool must be initialized before spawning enemies.");
            }

            if (config == null || activeEnemies.Count >= activeEnemyLimit)
            {
                return null;
            }

            EnemyController enemy = inactiveEnemies.Count > 0
                ? inactiveEnemies.Pop()
                : CreateEnemy();
            enemy.PoolIndex = activeEnemies.Count;
            activeEnemies.Add(enemy);
            spawnSequence++;
            enemy.Activate(
                this,
                targetRegistry,
                config,
                worldPosition,
                waveIndex,
                spawnSequence);
            EnemySpawned?.Invoke(enemy);
            return enemy;
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized
                || !SimulationEnabled
                || !worldScroller.ScrollingEnabled)
            {
                return;
            }

            for (int index = activeEnemies.Count - 1; index >= 0; index--)
            {
                activeEnemies[index].Tick(
                    deltaTime,
                    worldScroller.CurrentScrollSpeed,
                    true,
                    squadController,
                    contactPadding,
                    passBehindDistance);
            }
        }

        public void SetSimulationEnabled(bool enabled)
        {
            SimulationEnabled = enabled;
        }

        public void ResetPool()
        {
            while (activeEnemies.Count > 0)
            {
                Release(
                    activeEnemies[activeEnemies.Count - 1],
                    EnemyReleaseReason.Reset);
            }

            spawnSequence = 0;
        }

        public void CopyActiveEnemiesTo(List<EnemyController> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < activeEnemies.Count; index++)
            {
                destination.Add(activeEnemies[index]);
            }
        }

        internal void NotifyEnemyKilled(EnemyController enemy)
        {
            EnemyKilled?.Invoke(enemy);
        }

        internal void HandleEnemyContact(EnemyController enemy)
        {
            if (enemy == null || enemy.Config == null)
            {
                return;
            }

            int casualties = squadController.ApplyCasualties(
                enemy.Config.CasualtiesOnContact);
            EnemyReachedSquad?.Invoke(enemy, casualties);
            Release(enemy, EnemyReleaseReason.ReachedSquad);
        }

        internal void Release(
            EnemyController enemy,
            EnemyReleaseReason reason)
        {
            if (enemy == null)
            {
                return;
            }

            int releaseIndex = enemy.PoolIndex;
            int lastIndex = activeEnemies.Count - 1;
            if (releaseIndex < 0
                || releaseIndex > lastIndex
                || !ReferenceEquals(activeEnemies[releaseIndex], enemy))
            {
                return;
            }

            int waveIndex = enemy.WaveIndex;
            EnemyController lastEnemy = activeEnemies[lastIndex];
            activeEnemies[releaseIndex] = lastEnemy;
            lastEnemy.PoolIndex = releaseIndex;
            activeEnemies.RemoveAt(lastIndex);
            enemy.ReturnToPoolState();
            inactiveEnemies.Push(enemy);
            EnemyRemoved?.Invoke(waveIndex, reason);
        }

        private EnemyController CreateEnemy()
        {
            EnemyController enemy = Instantiate(
                enemyPrefab,
                enemyContainer != null ? enemyContainer : transform);
            enemy.gameObject.SetActive(false);
            TotalCreated++;
            return enemy;
        }
    }
}
