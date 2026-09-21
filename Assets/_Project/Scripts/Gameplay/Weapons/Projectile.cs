using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Gates;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        private ProjectilePool owner;
        private TargetRegistry registry;
        private Vector3 direction;
        private float speed;
        private float remainingLifetime;
        private float radius;
        private float damage;
        private int remainingPenetrations;
        private ITargetable lastHitTarget;

        public bool IsLaunched { get; private set; }

        public int PoolIndex { get; set; } = -1;

        private void Update()
        {
            if (IsLaunched && owner != null && owner.SimulationEnabled)
            {
                Tick(Time.deltaTime);
            }
        }

        public void Launch(
            ProjectilePool projectilePool,
            TargetRegistry targetRegistry,
            Vector3 origin,
            Vector3 firingDirection,
            WeaponRuntimeStats stats)
        {
            owner = projectilePool;
            registry = targetRegistry;
            direction = firingDirection.sqrMagnitude > 0.000001f
                ? firingDirection.normalized
                : Vector3.forward;
            speed = stats.ProjectileSpeed;
            remainingLifetime = stats.ProjectileLifetime;
            radius = stats.ProjectileRadius;
            damage = stats.Damage;
            remainingPenetrations = stats.PenetrationCount;
            lastHitTarget = null;
            transform.SetPositionAndRotation(
                origin,
                Quaternion.LookRotation(direction, Vector3.up));
            IsLaunched = true;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            if (!IsLaunched || deltaTime <= 0f)
            {
                return;
            }

            float safeDeltaTime = Mathf.Min(deltaTime, remainingLifetime);
            Vector3 start = transform.position;
            Vector3 end = start + (direction * speed * safeDeltaTime);
            remainingLifetime -= deltaTime;

            if (registry != null
                && registry.TryFindFirstSegmentHit(
                    start,
                    end,
                    radius,
                    lastHitTarget,
                    out ITargetable hitTarget))
            {
                lastHitTarget = hitTarget;
                if (hitTarget is IShootableGate gate)
                {
                    gate.ApplyProjectileHit();
                    owner.Release(this);
                    return;
                }

                if (hitTarget is IDamageable damageable)
                {
                    damageable.ApplyDamage(damage);
                }

                if (remainingPenetrations <= 0)
                {
                    owner.Release(this);
                    return;
                }

                remainingPenetrations--;
            }

            transform.position = end;
            if (remainingLifetime <= 0f)
            {
                owner.Release(this);
            }
        }

        public void ReturnToPoolState()
        {
            IsLaunched = false;
            PoolIndex = -1;
            owner = null;
            registry = null;
            lastHitTarget = null;
            gameObject.SetActive(false);
        }
    }
}
