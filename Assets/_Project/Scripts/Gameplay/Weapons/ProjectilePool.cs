using System;
using System.Collections.Generic;
using ProjectGatefront.Gameplay.Combat;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform projectileContainer;

        private readonly Stack<Projectile> inactiveProjectiles =
            new Stack<Projectile>(128);
        private readonly List<Projectile> activeProjectiles =
            new List<Projectile>(256);
        private TargetRegistry registry;

        public event Action<Projectile> ProjectileReleased;

        public bool IsInitialized { get; private set; }

        public bool SimulationEnabled { get; private set; }

        public int TotalCreated { get; private set; }

        public int ActiveProjectileCount => activeProjectiles.Count;

        public int InactiveProjectileCount => inactiveProjectiles.Count;

        public void Configure(Projectile prefab, Transform container)
        {
            projectilePrefab = prefab;
            projectileContainer = container;
        }

        public void Initialize(
            TargetRegistry targetRegistry,
            WeaponRuntimeStats stats,
            int expectedShooterCount)
        {
            if (IsInitialized)
            {
                return;
            }

            registry = targetRegistry != null
                ? targetRegistry
                : throw new ArgumentNullException(nameof(targetRegistry));
            if (projectilePrefab == null)
            {
                throw new InvalidOperationException(
                    "ProjectilePool requires a Projectile prefab.");
            }

            if (projectileContainer == null)
            {
                projectileContainer = transform;
            }

            int prewarmCount = WeaponMath.CalculateProjectilePrewarmCapacity(
                stats,
                expectedShooterCount);
            for (int index = 0; index < prewarmCount; index++)
            {
                inactiveProjectiles.Push(CreateProjectile());
            }

            SimulationEnabled = true;
            IsInitialized = true;
        }

        public Projectile Acquire(
            Vector3 origin,
            Vector3 direction,
            WeaponRuntimeStats stats)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "ProjectilePool must be initialized before acquiring projectiles.");
            }

            Projectile projectile = inactiveProjectiles.Count > 0
                ? inactiveProjectiles.Pop()
                : CreateProjectile();
            projectile.PoolIndex = activeProjectiles.Count;
            activeProjectiles.Add(projectile);
            projectile.Launch(this, registry, origin, direction, stats);
            return projectile;
        }

        public void Release(Projectile projectile)
        {
            if (projectile == null || !projectile.IsLaunched)
            {
                return;
            }

            int releaseIndex = projectile.PoolIndex;
            int lastIndex = activeProjectiles.Count - 1;
            if (releaseIndex < 0 || releaseIndex > lastIndex)
            {
                return;
            }

            Projectile lastProjectile = activeProjectiles[lastIndex];
            activeProjectiles[releaseIndex] = lastProjectile;
            lastProjectile.PoolIndex = releaseIndex;
            activeProjectiles.RemoveAt(lastIndex);
            ProjectileReleased?.Invoke(projectile);
            projectile.ReturnToPoolState();
            inactiveProjectiles.Push(projectile);
        }

        public void SetSimulationEnabled(bool enabled)
        {
            SimulationEnabled = enabled;
        }

        public void ResetPool()
        {
            while (activeProjectiles.Count > 0)
            {
                Release(activeProjectiles[activeProjectiles.Count - 1]);
            }
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(
                projectilePrefab,
                projectileContainer != null ? projectileContainer : transform);
            projectile.gameObject.SetActive(false);
            TotalCreated++;
            return projectile;
        }
    }
}
