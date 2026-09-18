using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class SquadWeaponSystem : MonoBehaviour
    {
        [SerializeField] private WeaponConfig equippedWeapon;
        [SerializeField] private SquadController squadController;
        [SerializeField] private TargetRegistry targetRegistry;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private WorldScroller worldScroller;
        [SerializeField] private List<CombatTargetDummy> testTargets =
            new List<CombatTargetDummy>();
        [SerializeField, Range(0.05f, 0.5f)] private float targetRefreshInterval = 0.1f;

        [Header("Development")]
        [SerializeField] private bool debugOverlayEnabled = true;

        private readonly List<SquadUnit> activeUnits = new List<SquadUnit>(50);
        private readonly List<WeaponController> activeWeapons =
            new List<WeaponController>(50);
        private WeaponRuntimeStats runtimeStats;
        private ITargetable currentTarget;
        private float targetRefreshRemaining;
        private bool combatEnabled;

        public bool IsInitialized { get; private set; }

        public int ShooterCount => activeWeapons.Count;

        public int ActiveTargetCount => targetRegistry == null
            ? 0
            : targetRegistry.ActiveTargetCount;

        public int ActiveProjectileCount => projectilePool == null
            ? 0
            : projectilePool.ActiveProjectileCount;

        public int TotalProjectileCapacity => projectilePool == null
            ? 0
            : projectilePool.TotalCreated;

        public int TotalShotsFired
        {
            get
            {
                int total = 0;
                for (int index = 0; index < activeWeapons.Count; index++)
                {
                    total += activeWeapons[index].ShotCount;
                }

                return total;
            }
        }

        public string CurrentWeaponName => runtimeStats.DisplayName;

        public ITargetable CurrentTarget => currentTarget;

        public ProjectilePool ProjectilePool => projectilePool;

        public TargetRegistry TargetRegistry => targetRegistry;

        private void Update()
        {
            if (!IsInitialized || !combatEnabled)
            {
                return;
            }

            targetRefreshRemaining -= Time.deltaTime;
            if (targetRefreshRemaining > 0f)
            {
                return;
            }

            targetRefreshRemaining = targetRefreshInterval;
            ITargetable selectedTarget = targetRegistry.FindClosestForwardTarget(
                squadController.transform.position,
                squadController.transform.forward,
                runtimeStats.Range,
                runtimeStats.TargetingCosine);
            if (!ReferenceEquals(selectedTarget, currentTarget))
            {
                SetCurrentTarget(selectedTarget);
            }
        }

        public void Configure(
            WeaponConfig weaponConfig,
            SquadController squad,
            TargetRegistry registry,
            ProjectilePool pool,
            WorldScroller scroller,
            IList<CombatTargetDummy> targets)
        {
            equippedWeapon = weaponConfig;
            squadController = squad;
            targetRegistry = registry;
            projectilePool = pool;
            worldScroller = scroller;
            testTargets.Clear();
            if (targets == null)
            {
                return;
            }

            for (int index = 0; index < targets.Count; index++)
            {
                if (targets[index] != null && !testTargets.Contains(targets[index]))
                {
                    testTargets.Add(targets[index]);
                }
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (equippedWeapon == null
                || squadController == null
                || targetRegistry == null
                || projectilePool == null
                || worldScroller == null)
            {
                throw new InvalidOperationException(
                    "SquadWeaponSystem is missing required references.");
            }

            runtimeStats = WeaponRuntimeStats.FromConfig(equippedWeapon);
            projectilePool.Initialize(
                targetRegistry,
                runtimeStats,
                squadController.CurrentUnitCount);

            for (int index = 0; index < testTargets.Count; index++)
            {
                CombatTargetDummy target = testTargets[index];
                if (target != null)
                {
                    target.Initialize(targetRegistry, worldScroller);
                }
            }

            squadController.SquadCountChanged += HandleSquadCountChanged;
            IsInitialized = true;
            RefreshShooters();
            SetCombatEnabled(true);
        }

        public void EquipWeapon(
            WeaponConfig weaponConfig,
            WeaponStatModifiers modifiers)
        {
            equippedWeapon = weaponConfig != null
                ? weaponConfig
                : throw new ArgumentNullException(nameof(weaponConfig));
            runtimeStats = WeaponRuntimeStats.FromConfig(
                equippedWeapon,
                modifiers);
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                activeWeapons[index].Equip(equippedWeapon, runtimeStats);
            }

            targetRefreshRemaining = 0f;
        }

        public void SetCombatEnabled(bool enabled)
        {
            combatEnabled = enabled;
            projectilePool.SetSimulationEnabled(enabled);
            if (!enabled)
            {
                SetCurrentTarget(null);
            }

            for (int index = 0; index < activeWeapons.Count; index++)
            {
                activeWeapons[index].SetFiringEnabled(enabled);
            }

            if (enabled)
            {
                targetRefreshRemaining = 0f;
            }
        }

        public void ResetCombat()
        {
            projectilePool.ResetPool();
            targetRegistry.Clear();
            for (int index = 0; index < testTargets.Count; index++)
            {
                if (testTargets[index] != null)
                {
                    testTargets[index].ResetTarget();
                }
            }

            SetCurrentTarget(null);
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                activeWeapons[index].ResetCooldown(
                    CalculateCooldownPhase(index));
                activeWeapons[index].SetFiringEnabled(combatEnabled);
            }

            targetRefreshRemaining = 0f;
        }

        public void CopyTestTargetsTo(List<CombatTargetDummy> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < testTargets.Count; index++)
            {
                destination.Add(testTargets[index]);
            }
        }

        public void CopyActiveWeaponsTo(List<WeaponController> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                destination.Add(activeWeapons[index]);
            }
        }

        private void HandleSquadCountChanged(int _)
        {
            RefreshShooters();
        }

        private void RefreshShooters()
        {
            squadController.CopyActiveUnitsTo(activeUnits);
            activeWeapons.Clear();
            for (int index = 0; index < activeUnits.Count; index++)
            {
                WeaponController weapon = activeUnits[index]
                    .GetComponent<WeaponController>();
                if (weapon == null)
                {
                    Debug.LogError(
                        "Every active SquadUnit requires a WeaponController.",
                        activeUnits[index]);
                    continue;
                }

                weapon.Initialize(
                    equippedWeapon,
                    runtimeStats,
                    projectilePool,
                    index + 1,
                    CalculateCooldownPhase(index));
                weapon.SetFiringEnabled(combatEnabled);
                weapon.SetTarget(currentTarget);
                activeWeapons.Add(weapon);
            }
        }

        private void SetCurrentTarget(ITargetable selectedTarget)
        {
            currentTarget = selectedTarget;
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                activeWeapons[index].SetTarget(selectedTarget);
            }
        }

        private static float CalculateCooldownPhase(int shooterIndex)
        {
            return ((shooterIndex * 37) % 100) / 100f;
        }

        private void OnDestroy()
        {
            if (squadController != null)
            {
                squadController.SquadCountChanged -= HandleSquadCountChanged;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!debugOverlayEnabled || !IsInitialized)
            {
                return;
            }

            GUI.Box(
                new Rect(12f, 96f, 310f, 84f),
                $"Weapon: {CurrentWeaponName}\nShooters: {ShooterCount}  "
                + $"Targets: {ActiveTargetCount}\nProjectiles: "
                + $"{ActiveProjectileCount}/{TotalProjectileCapacity}");
        }
#endif
    }
}
