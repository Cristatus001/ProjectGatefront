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
        [SerializeField, Range(1, 100)] private int maximumDistributedTargets = 100;

        [Header("Development")]
        [SerializeField] private bool debugOverlayEnabled = true;

        private readonly List<SquadUnit> activeUnits = new List<SquadUnit>(50);
        private readonly List<WeaponController> activeWeapons =
            new List<WeaponController>(50);
        private readonly List<ITargetable> targetCandidates =
            new List<ITargetable>(100);
        private readonly Dictionary<ITargetable, int> assignmentsThisPass =
            new Dictionary<ITargetable, int>(100);
        private readonly Dictionary<ITargetable, int> targetReservations =
            new Dictionary<ITargetable, int>(100);
        private readonly Dictionary<Projectile, ITargetable> projectileClaims =
            new Dictionary<Projectile, ITargetable>(512);
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

        public WeaponRuntimeStats CurrentRuntimeStats => runtimeStats;

        public ITargetable CurrentTarget => currentTarget;

        public int AssignedTargetCount => targetCandidates.Count;

        public int ReservedTargetCount => targetReservations.Count;

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
            targetRegistry.CollectClosestForwardTargets(
                squadController.transform.position,
                squadController.transform.forward,
                runtimeStats.Range,
                runtimeStats.TargetingCosine,
                maximumDistributedTargets,
                targetCandidates);
            AssignTargets();
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
                squadController.ConfiguredMaximumUnitCount);
            projectilePool.ProjectileReleased += HandleProjectileReleased;

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
                targetCandidates.Clear();
                AssignTargets();
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
            projectileClaims.Clear();
            targetReservations.Clear();
            targetRegistry.Clear();
            for (int index = 0; index < testTargets.Count; index++)
            {
                if (testTargets[index] != null)
                {
                    testTargets[index].ResetTarget();
                }
            }

            targetCandidates.Clear();
            AssignTargets();
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

        public void CopyTargetCandidatesTo(List<ITargetable> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < targetCandidates.Count; index++)
            {
                destination.Add(targetCandidates[index]);
            }
        }

        public void ClearProjectiles()
        {
            projectilePool.ResetPool();
        }

        private void HandleSquadCountChanged(int _)
        {
            RefreshShooters();
        }

        private void RefreshShooters()
        {
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                activeWeapons[index].ProjectileCommitted -= HandleProjectileCommitted;
            }

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
                weapon.ProjectileCommitted += HandleProjectileCommitted;
                weapon.SetFiringEnabled(combatEnabled);
                activeWeapons.Add(weapon);
            }

            AssignTargets();
        }

        private void AssignTargets()
        {
            assignmentsThisPass.Clear();
            currentTarget = null;
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                ITargetable selected = FindPreferredTarget();
                activeWeapons[index].SetTarget(selected);
                if (selected != null)
                {
                    assignmentsThisPass.TryGetValue(selected, out int assigned);
                    assignmentsThisPass[selected] = assigned + 1;
                    if (currentTarget == null)
                    {
                        currentTarget = selected;
                    }
                }
            }
        }

        private ITargetable FindPreferredTarget()
        {
            ITargetable selected = null;
            int bestScore = int.MaxValue;
            for (int index = 0; index < targetCandidates.Count; index++)
            {
                ITargetable candidate = targetCandidates[index];
                if (candidate == null || !candidate.IsTargetable)
                {
                    continue;
                }

                assignmentsThisPass.TryGetValue(candidate, out int assigned);
                targetReservations.TryGetValue(candidate, out int reserved);
                int score = (reserved > 0 ? 1000 : 0) + assigned;
                if (score < bestScore)
                {
                    bestScore = score;
                    selected = candidate;
                }
            }

            return selected;
        }

        private void HandleProjectileCommitted(
            ITargetable target,
            Projectile projectile)
        {
            if (target == null || projectile == null)
            {
                return;
            }

            projectileClaims[projectile] = target;
            targetReservations.TryGetValue(target, out int count);
            targetReservations[target] = count + 1;
            targetRefreshRemaining = 0f;
        }

        private void HandleProjectileReleased(Projectile projectile)
        {
            if (!projectileClaims.TryGetValue(projectile, out ITargetable target))
            {
                return;
            }

            projectileClaims.Remove(projectile);
            if (targetReservations.TryGetValue(target, out int count))
            {
                if (count <= 1)
                {
                    targetReservations.Remove(target);
                }
                else
                {
                    targetReservations[target] = count - 1;
                }
            }

            targetRefreshRemaining = 0f;
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

            if (projectilePool != null)
            {
                projectilePool.ProjectileReleased -= HandleProjectileReleased;
            }

            for (int index = 0; index < activeWeapons.Count; index++)
            {
                activeWeapons[index].ProjectileCommitted -= HandleProjectileCommitted;
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
