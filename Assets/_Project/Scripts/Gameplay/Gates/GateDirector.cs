using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Gates
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class GateDirector : MonoBehaviour
    {
        private sealed class EncounterRuntime
        {
            public GateEncounterDefinition Definition;
            public GateController Shootable;
            public GateController Divide;
            public float PreviousZ;
            public bool Resolved;
        }

        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private GateController gatePrefab;
        [SerializeField] private Transform gateContainer;
        [SerializeField] private WorldScroller worldScroller;
        [SerializeField] private TargetRegistry targetRegistry;
        [SerializeField] private SquadController squadController;
        [SerializeField] private ContinuousEnemyDirector enemyDirector;
        [SerializeField] private PlayerConfig playerConfig;

        private readonly List<EncounterRuntime> encounters = new List<EncounterRuntime>(8);
        private readonly List<GateController> pool = new List<GateController>(12);
        private int nextEncounter;
        private bool timelineEnabled = true;

        public event Action<GateResolution> GateResolved;

        public bool IsInitialized { get; private set; }
        public bool SimulationEnabled { get; private set; }
        public int ResolvedCount { get; private set; }
        public int ActiveGateCount { get; private set; }
        public int NextEncounterIndex => nextEncounter;

        private void Update()
        {
            Tick();
        }

        public void Configure(
            LevelConfig config,
            GateController prefab,
            Transform container,
            WorldScroller scroller,
            TargetRegistry registry,
            SquadController squad,
            ContinuousEnemyDirector enemies,
            PlayerConfig track)
        {
            levelConfig = config;
            gatePrefab = prefab;
            gateContainer = container;
            worldScroller = scroller;
            targetRegistry = registry;
            squadController = squad;
            enemyDirector = enemies;
            playerConfig = track;
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (levelConfig == null || gatePrefab == null || gateContainer == null
                || worldScroller == null || targetRegistry == null
                || squadController == null || enemyDirector == null || playerConfig == null)
            {
                throw new InvalidOperationException("GateDirector is missing required references.");
            }

            encounters.Clear();
            int requiredGates = 0;
            foreach (GateEncounterDefinition definition in levelConfig.GateEncounters)
            {
                if (definition == null)
                {
                    continue;
                }

                encounters.Add(new EncounterRuntime { Definition = definition });
                requiredGates += definition.Layout == GateEncounterLayout.ShootableVsDivide ? 2 : 1;
            }

            encounters.Sort((left, right) => left.Definition.Distance.CompareTo(right.Definition.Distance));
            for (int index = 0; index < requiredGates; index++)
            {
                GateController gate = Instantiate(gatePrefab, gateContainer);
                gate.name = "PooledGate_" + index;
                gate.gameObject.SetActive(false);
                pool.Add(gate);
            }

            IsInitialized = true;
            ResetGates();
            SimulationEnabled = true;
        }

        public void Tick()
        {
            if (!IsInitialized || !SimulationEnabled || !worldScroller.ScrollingEnabled)
            {
                return;
            }

            float distance = worldScroller.DistanceTravelled;
            while (timelineEnabled && nextEncounter < encounters.Count
                && distance >= encounters[nextEncounter].Definition.Distance
                    - encounters[nextEncounter].Definition.SpawnDepth)
            {
                SpawnEncounter(nextEncounter++);
            }

            float density = 1f;
            for (int index = 0; index < encounters.Count; index++)
            {
                GateEncounterDefinition definition = encounters[index].Definition;
                if (distance >= definition.Distance - definition.ReadabilityBeforeDistance
                    && distance <= definition.Distance + definition.ReadabilityAfterDistance)
                {
                    density = Mathf.Min(density, definition.EnemyDensityMultiplier);
                }

                EncounterRuntime encounter = encounters[index];
                if (encounter.Shootable == null || encounter.Resolved)
                {
                    continue;
                }

                float currentZ = encounter.Shootable.WorldZ;
                float planeZ = squadController.CombatContactZ;
                if (GateMath.CrossedSelectionPlane(encounter.PreviousZ, currentZ, planeZ)
                    || currentZ <= planeZ)
                {
                    Resolve(encounter);
                }
                else
                {
                    encounter.PreviousZ = currentZ;
                }
            }

            enemyDirector.SetSpawnIntervalMultiplier(1f / density);
        }

        public void SetSimulationEnabled(bool enabled)
        {
            SimulationEnabled = enabled;
        }

        public void SetTimelineEnabled(bool enabled)
        {
            timelineEnabled = enabled;
        }

        public void SpawnEncounterNow(int index)
        {
            if (!IsInitialized || index < 0 || index >= encounters.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            SpawnEncounter(index);
            nextEncounter = Mathf.Max(nextEncounter, index + 1);
        }

        public void ResetGates()
        {
            for (int index = 0; index < pool.Count; index++)
            {
                pool[index].Deactivate();
            }

            for (int index = 0; index < encounters.Count; index++)
            {
                encounters[index].Shootable = null;
                encounters[index].Divide = null;
                encounters[index].Resolved = false;
                encounters[index].PreviousZ = 0f;
            }

            nextEncounter = 0;
            ResolvedCount = 0;
            ActiveGateCount = 0;
            timelineEnabled = true;
            enemyDirector?.SetSpawnIntervalMultiplier(1f);
        }

        public void CopyActiveGatesTo(List<GateController> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < pool.Count; index++)
            {
                if (pool[index].IsActive)
                {
                    destination.Add(pool[index]);
                }
            }
        }

        private void SpawnEncounter(int index)
        {
            EncounterRuntime encounter = encounters[index];
            if (encounter.Shootable != null || encounter.Resolved)
            {
                return;
            }

            GateEncounterDefinition definition = encounter.Definition;
            float usableHalfWidth = Mathf.Max(1f,
                playerConfig.TrackHalfWidth - playerConfig.TrackBoundaryPadding);
            bool paired = definition.Layout == GateEncounterLayout.ShootableVsDivide;
            float width = paired ? usableHalfWidth - 0.15f : usableHalfWidth * 2f - 0.15f;
            float laneX = paired ? -usableHalfWidth * 0.5f : 0f;
            float worldZ = squadController.CombatContactZ
                + definition.Distance - worldScroller.DistanceTravelled;
            encounter.Shootable = GetAvailableGate();
            encounter.Shootable.Activate(definition, GateKind.ShootableAdditive,
                worldScroller, targetRegistry, new Vector3(laneX, 0f, worldZ),
                width, squadController.CombatContactZ - 1f);
            ActiveGateCount++;

            if (paired)
            {
                encounter.Divide = GetAvailableGate();
                encounter.Divide.Activate(definition, GateKind.Divide,
                    worldScroller, targetRegistry,
                    new Vector3(-laneX, 0f, worldZ), width,
                    squadController.CombatContactZ - 1f);
                ActiveGateCount++;
            }

            encounter.PreviousZ = worldZ;
        }

        private GateController GetAvailableGate()
        {
            for (int index = 0; index < pool.Count; index++)
            {
                if (!pool[index].IsActive)
                {
                    return pool[index];
                }
            }

            throw new InvalidOperationException("Gate pool capacity was exhausted.");
        }

        private void Resolve(EncounterRuntime encounter)
        {
            GateController selected = encounter.Divide == null
                || GateMath.SelectNearestLane(squadController.transform.position.x,
                    encounter.Shootable.WorldX, encounter.Divide.WorldX) == 0
                    ? encounter.Shootable : encounter.Divide;
            int start = selected.Kind == GateKind.Divide
                ? selected.DivideAmount : selected.StartingValue;
            selected.LockValue();
            int final = selected.Kind == GateKind.Divide
                ? selected.DivideAmount
                : selected.CurrentValue;
            int before = squadController.CurrentUnitCount;
            int after = selected.Kind == GateKind.Divide
                ? GateMath.ApplyArithmetic(before, GateArithmeticOperation.Divide,
                    selected.DivideAmount, squadController.ConfiguredMaximumUnitCount)
                : GateMath.ApplyArithmetic(before, GateArithmeticOperation.Add,
                    final, squadController.ConfiguredMaximumUnitCount);
            if (after > before)
            {
                squadController.AddUnits(after - before);
            }
            else if (after < before)
            {
                squadController.RemoveUnits(before - after);
            }

            encounter.Resolved = true;
            ResolvedCount++;
            encounter.Shootable.Deactivate();
            ActiveGateCount--;
            if (encounter.Divide != null)
            {
                encounter.Divide.Deactivate();
                ActiveGateCount--;
            }

            GateResolved?.Invoke(new GateResolution(start, final, before,
                squadController.CurrentUnitCount, selected.Kind));
        }
    }
}
