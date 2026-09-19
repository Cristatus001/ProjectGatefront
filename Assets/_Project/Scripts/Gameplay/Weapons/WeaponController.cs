using System;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform weaponMount;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField, Min(0.01f)] private float muzzleFlashDuration = 0.045f;

        private ProjectilePool projectilePool;
        private WeaponConfig equippedConfig;
        private WeaponRuntimeStats stats;
        private ITargetable target;
        private GameObject weaponVisualInstance;
        private GameObject weaponVisualSource;
        private float cooldownRemaining;
        private float muzzleFlashRemaining;
        private int deterministicSeed;
        private int shotSequence;
        private bool firingEnabled;

        public event Action<ITargetable, Projectile> ProjectileCommitted;

        public bool IsInitialized { get; private set; }

        public int ShotCount { get; private set; }

        public int ProjectileLaunchCount { get; private set; }

        public Transform FirePoint => firePoint;

        public ITargetable CurrentTarget => target;

        private void Update()
        {
            if (!IsInitialized || !firingEnabled)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
            UpdateMuzzleFlash(deltaTime);
            UpdateWeaponAim(deltaTime);

            if (target == null
                || !target.IsTargetable
                || !TargetingMath.IsValidForwardTarget(
                    firePoint.position,
                    transform.forward,
                    target.AimPosition,
                    stats.Range,
                    stats.TargetingCosine)
                || cooldownRemaining > 0f)
            {
                return;
            }

            Fire();
            cooldownRemaining = stats.FireInterval;
        }

        public void Configure(
            Transform mount,
            Transform projectileOrigin,
            GameObject flash)
        {
            weaponMount = mount;
            firePoint = projectileOrigin;
            muzzleFlash = flash;
        }

        public void Initialize(
            WeaponConfig weaponConfig,
            WeaponRuntimeStats runtimeStats,
            ProjectilePool pool,
            int shooterSeed,
            float initialCooldownPhase)
        {
            if (firePoint == null)
            {
                throw new InvalidOperationException(
                    "WeaponController requires a FirePoint.");
            }

            projectilePool = pool != null
                ? pool
                : throw new ArgumentNullException(nameof(pool));
            deterministicSeed = shooterSeed;
            IsInitialized = true;
            Equip(weaponConfig, runtimeStats);
            ResetCooldown(initialCooldownPhase);
        }

        public void Equip(
            WeaponConfig weaponConfig,
            WeaponRuntimeStats runtimeStats)
        {
            equippedConfig = weaponConfig != null
                ? weaponConfig
                : throw new ArgumentNullException(nameof(weaponConfig));
            stats = runtimeStats;
            ApplyWeaponVisual(equippedConfig.WeaponVisualPrefab);
        }

        public void SetTarget(ITargetable newTarget)
        {
            target = newTarget;
        }

        public void SetFiringEnabled(bool enabled)
        {
            firingEnabled = enabled;
            if (!enabled)
            {
                target = null;
                SetMuzzleFlashActive(false);
            }
        }

        public void ResetCooldown(float normalizedPhase)
        {
            float phase = Mathf.Repeat(normalizedPhase, 1f);
            cooldownRemaining = float.IsInfinity(stats.FireInterval)
                ? float.PositiveInfinity
                : stats.FireInterval * phase;
            shotSequence = 0;
            ShotCount = 0;
            ProjectileLaunchCount = 0;
            target = null;
            SetMuzzleFlashActive(false);
        }

        private void Fire()
        {
            Vector3 baseDirection = target.AimPosition - firePoint.position;
            if (baseDirection.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            for (int projectileIndex = 0;
                projectileIndex < stats.ProjectilesPerShot;
                projectileIndex++)
            {
                float spreadAngle = CalculateDeterministicSpread(
                    projectileIndex,
                    stats.ProjectilesPerShot,
                    stats.SpreadDegrees);
                Vector3 direction = Quaternion.AngleAxis(spreadAngle, Vector3.up)
                    * baseDirection.normalized;
                Projectile projectile = projectilePool.Acquire(
                    firePoint.position,
                    direction,
                    stats);
                ProjectileCommitted?.Invoke(target, projectile);
                ProjectileLaunchCount++;
            }

            ShotCount++;
            shotSequence++;
            muzzleFlashRemaining = muzzleFlashDuration;
            SetMuzzleFlashActive(true);
        }

        private float CalculateDeterministicSpread(
            int projectileIndex,
            int projectileCount,
            float maximumSpread)
        {
            if (maximumSpread <= 0f)
            {
                return 0f;
            }

            if (projectileCount > 1)
            {
                float centeredIndex = projectileIndex
                    - ((projectileCount - 1) * 0.5f);
                float divisor = Mathf.Max(1f, (projectileCount - 1) * 0.5f);
                return (centeredIndex / divisor) * maximumSpread;
            }

            int hash = (deterministicSeed * 73856093)
                ^ (shotSequence * 19349663);
            float normalized = (hash & 1023) / 1023f;
            return ((normalized * 2f) - 1f) * maximumSpread;
        }

        private void UpdateMuzzleFlash(float deltaTime)
        {
            if (muzzleFlashRemaining <= 0f)
            {
                return;
            }

            muzzleFlashRemaining -= deltaTime;
            if (muzzleFlashRemaining <= 0f)
            {
                SetMuzzleFlashActive(false);
            }
        }

        private void UpdateWeaponAim(float deltaTime)
        {
            if (weaponMount == null)
            {
                return;
            }

            Quaternion desiredWorldRotation = transform.rotation;
            if (target != null && target.IsTargetable)
            {
                Vector3 aimDirection = target.AimPosition - weaponMount.position;
                aimDirection.y = 0f;
                if (aimDirection.sqrMagnitude > 0.000001f)
                {
                    desiredWorldRotation = Quaternion.LookRotation(
                        aimDirection,
                        Vector3.up);
                }
            }

            weaponMount.rotation = Quaternion.RotateTowards(
                weaponMount.rotation,
                desiredWorldRotation,
                540f * deltaTime);
        }

        private void ApplyWeaponVisual(GameObject visualPrefab)
        {
            if (weaponMount == null
                || visualPrefab == null
                || ReferenceEquals(weaponVisualSource, visualPrefab))
            {
                return;
            }

            if (weaponVisualInstance != null)
            {
                weaponVisualInstance.SetActive(false);
            }

            weaponVisualSource = visualPrefab;
            weaponVisualInstance = Instantiate(visualPrefab, weaponMount);
            weaponVisualInstance.name = "EquippedWeaponVisual";
            weaponVisualInstance.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
        }

        private void SetMuzzleFlashActive(bool active)
        {
            muzzleFlashRemaining = active ? muzzleFlashRemaining : 0f;
            if (muzzleFlash != null && muzzleFlash.activeSelf != active)
            {
                muzzleFlash.SetActive(active);
            }
        }

        private void OnDisable()
        {
            target = null;
            SetMuzzleFlashActive(false);
        }
    }
}
