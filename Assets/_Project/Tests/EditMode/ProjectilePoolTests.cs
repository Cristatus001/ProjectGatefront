using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Weapons;
using UnityEngine;

namespace ProjectGatefront.Tests
{
    public sealed class ProjectilePoolTests
    {
        [Test]
        public void ReleasedProjectile_IsReusedWithoutGrowingPool()
        {
            var root = new GameObject("ProjectilePoolTest");
            var prefabObject = new GameObject("ProjectilePrefab");
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            try
            {
                TargetRegistry registry = root.AddComponent<TargetRegistry>();
                ProjectilePool pool = root.AddComponent<ProjectilePool>();
                Projectile prefab = prefabObject.AddComponent<Projectile>();
                pool.Configure(prefab, root.transform);
                WeaponRuntimeStats stats = WeaponRuntimeStats.FromConfig(config);
                pool.Initialize(registry, stats, 1);

                int capacity = pool.TotalCreated;
                Assert.That(capacity, Is.GreaterThanOrEqualTo(16));
                Assert.That(pool.InactiveProjectileCount, Is.EqualTo(capacity));
                Projectile first = pool.Acquire(
                    Vector3.zero,
                    Vector3.forward,
                    stats);
                Assert.That(pool.ActiveProjectileCount, Is.EqualTo(1));
                pool.Release(first);
                Assert.That(pool.ActiveProjectileCount, Is.Zero);

                Projectile second = pool.Acquire(
                    Vector3.zero,
                    Vector3.forward,
                    stats);
                Assert.That(second, Is.SameAs(first));
                Assert.That(pool.TotalCreated, Is.EqualTo(capacity));
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(prefabObject);
                Object.DestroyImmediate(root);
            }
        }
    }
}
