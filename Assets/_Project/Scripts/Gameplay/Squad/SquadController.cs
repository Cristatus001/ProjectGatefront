using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Player;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace ProjectGatefront.Gameplay.Squad
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class SquadController : MonoBehaviour
    {
        [SerializeField] private SquadConfig config;
        [SerializeField] private SquadUnit unitPrefab;
        [SerializeField] private Transform formationRoot;
        [SerializeField] private FormationType currentFormation = FormationType.Compact;

        [Header("Development")]
        [SerializeField] private bool debugControlsEnabled = true;
        [SerializeField] private bool debugOverlayEnabled = true;
        [SerializeField] private bool drawFormationGizmos = true;

        private readonly List<SquadUnit> activeUnits = new List<SquadUnit>();
        private readonly List<FormationSlot> formationSlots = new List<FormationSlot>();
        private Stack<SquadUnit> pooledUnits;
        private PlayerConfig playerConfig;
        private PlayerController controlRoot;
        private float idealLocalMinX;
        private float idealLocalMaxX;
        private float idealLocalMaxZ;

        public event Action<int> SquadCountChanged;

        public event Action<FormationType> FormationChanged;

        public event Action<int> CasualtiesApplied;

        public event Action SquadDefeated;

        public bool IsInitialized { get; private set; }

        public int CurrentUnitCount => IsInitialized
            ? activeUnits.Count
            : 0;

        public int ActiveUnitCount => activeUnits.Count;

        public int FormationSlotCount => formationSlots.Count;

        public int FormationRevision { get; private set; }

        public bool IsDefeated { get; private set; }

        public float MinimumRootX { get; private set; }

        public float MaximumRootX { get; private set; }

        public float UnitUsableTrackHalfWidth => playerConfig == null
            || config == null
                ? 0f
                : FormationBoundaryMath.CalculateUsableTrackHalfWidth(
                    playerConfig.TrackHalfWidth,
                    config.UnitBoundaryRadius,
                    playerConfig.TrackBoundaryPadding);

        public float CurrentWorldMinimumX => transform.position.x
            + idealLocalMinX
            - (config == null ? 0f : config.UnitBoundaryRadius);

        public float CurrentWorldMaximumX => transform.position.x
            + idealLocalMaxX
            + (config == null ? 0f : config.UnitBoundaryRadius);

        public float CombatContactZ => transform.position.z
            + idealLocalMaxZ
            + (config == null ? 0f : config.UnitBoundaryRadius);

        public int ConfiguredStartingUnitCount => config == null
            ? 0
            : Mathf.Clamp(config.StartingUnitCount, 1, config.MaxSquadSize);

        public FormationType CurrentFormation => currentFormation;

        public void Configure(
            SquadConfig squadConfig,
            SquadUnit squadUnitPrefab,
            Transform squadFormationRoot)
        {
            config = squadConfig;
            unitPrefab = squadUnitPrefab;
            formationRoot = squadFormationRoot;
        }

        public void Initialize(PlayerConfig trackConfig)
        {
            Initialize(
                config,
                unitPrefab,
                formationRoot,
                trackConfig,
                GetComponentInParent<PlayerController>());
        }

        public void Initialize(
            PlayerConfig trackConfig,
            PlayerController playerControlRoot)
        {
            Initialize(
                config,
                unitPrefab,
                formationRoot,
                trackConfig,
                playerControlRoot);
        }

        public void Initialize(
            SquadConfig squadConfig,
            SquadUnit squadUnitPrefab,
            Transform squadFormationRoot,
            PlayerConfig trackConfig)
        {
            Initialize(
                squadConfig,
                squadUnitPrefab,
                squadFormationRoot,
                trackConfig,
                null);
        }

        public void Initialize(
            SquadConfig squadConfig,
            SquadUnit squadUnitPrefab,
            Transform squadFormationRoot,
            PlayerConfig trackConfig,
            PlayerController playerControlRoot)
        {
            if (IsInitialized)
            {
                return;
            }

            config = squadConfig != null
                ? squadConfig
                : throw new ArgumentNullException(nameof(squadConfig));
            unitPrefab = squadUnitPrefab != null
                ? squadUnitPrefab
                : throw new ArgumentNullException(nameof(squadUnitPrefab));
            formationRoot = squadFormationRoot != null
                ? squadFormationRoot
                : throw new ArgumentNullException(nameof(squadFormationRoot));
            playerConfig = trackConfig != null
                ? trackConfig
                : throw new ArgumentNullException(nameof(trackConfig));
            controlRoot = playerControlRoot;

            int maximumUnitCount = Mathf.Max(1, config.MaxSquadSize);
            activeUnits.Capacity = Mathf.Max(
                activeUnits.Capacity,
                maximumUnitCount);
            formationSlots.Capacity = Mathf.Max(
                formationSlots.Capacity,
                maximumUnitCount);
            pooledUnits = new Stack<SquadUnit>(maximumUnitCount);
            IsInitialized = true;

            int startingUnitCount = ConfiguredStartingUnitCount;
            Prewarm(startingUnitCount);
            AddUnits(startingUnitCount);
        }

        public int AddUnit()
        {
            return AddUnits(1);
        }

        public int AddUnits(int count)
        {
            if (!IsInitialized || IsDefeated || count <= 0)
            {
                return 0;
            }

            int availableCapacity = Mathf.Max(
                0,
                config.MaxSquadSize - CurrentUnitCount);
            int addCount = Mathf.Min(count, availableCapacity);
            if (addCount <= 0)
            {
                return 0;
            }

            for (int index = 0; index < addCount; index++)
            {
                SquadUnit unit = AcquireUnit();
                unit.Activate(
                    formationRoot,
                    Vector3.zero,
                    config.FormationTransitionSpeed,
                    config.UnitRotationSpeed);
                activeUnits.Add(unit);
            }

            RebuildFormation();
            SquadCountChanged?.Invoke(CurrentUnitCount);
            return addCount;
        }

        public int RemoveUnit()
        {
            return RemoveUnits(1);
        }

        public int RemoveUnits(int count)
        {
            if (!IsInitialized || count <= 0)
            {
                return 0;
            }

            int removeCount = Mathf.Min(
                count,
                Mathf.Max(0, activeUnits.Count - 1));
            if (removeCount <= 0)
            {
                return 0;
            }

            DeactivateLastUnits(removeCount);

            RebuildFormation();
            SquadCountChanged?.Invoke(CurrentUnitCount);
            return removeCount;
        }

        public int ApplyCasualties(int count)
        {
            if (!IsInitialized || IsDefeated || count <= 0)
            {
                return 0;
            }

            int casualtyCount = Mathf.Min(count, activeUnits.Count);
            if (casualtyCount <= 0)
            {
                return 0;
            }

            DeactivateLastUnits(casualtyCount);
            RebuildFormation();
            SquadCountChanged?.Invoke(CurrentUnitCount);
            CasualtiesApplied?.Invoke(casualtyCount);

            if (activeUnits.Count == 0 && !IsDefeated)
            {
                IsDefeated = true;
                SquadDefeated?.Invoke();
            }

            return casualtyCount;
        }

        public void ResetSquad()
        {
            if (!IsInitialized)
            {
                return;
            }

            DeactivateLastUnits(activeUnits.Count);
            IsDefeated = false;
            int resetCount = ConfiguredStartingUnitCount;
            for (int index = 0; index < resetCount; index++)
            {
                SquadUnit unit = AcquireUnit();
                unit.Activate(
                    formationRoot,
                    Vector3.zero,
                    config.FormationTransitionSpeed,
                    config.UnitRotationSpeed);
                activeUnits.Add(unit);
            }

            RebuildFormation();
            SquadCountChanged?.Invoke(CurrentUnitCount);
        }

        public bool SetFormation(FormationType formationType)
        {
            if (!IsInitialized || currentFormation == formationType)
            {
                return false;
            }

            currentFormation = formationType;
            RebuildFormation();
            FormationChanged?.Invoke(currentFormation);
            return true;
        }

        public void RefreshFormation()
        {
            if (IsInitialized)
            {
                RebuildFormation();
            }
        }

        public void CopyActiveUnitsTo(List<SquadUnit> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < activeUnits.Count; index++)
            {
                destination.Add(activeUnits[index]);
            }
        }

        private void Prewarm(int count)
        {
            for (int index = 0; index < count; index++)
            {
                pooledUnits.Push(CreatePooledUnit());
            }
        }

        private SquadUnit AcquireUnit()
        {
            return pooledUnits.Count > 0
                ? pooledUnits.Pop()
                : CreatePooledUnit();
        }

        private SquadUnit CreatePooledUnit()
        {
            SquadUnit unit = Instantiate(unitPrefab, formationRoot);
            unit.gameObject.SetActive(false);
            return unit;
        }

        private void DeactivateLastUnits(int count)
        {
            for (int index = 0; index < count; index++)
            {
                int lastIndex = activeUnits.Count - 1;
                SquadUnit unit = activeUnits[lastIndex];
                activeUnits.RemoveAt(lastIndex);
                unit.Deactivate();
                pooledUnits.Push(unit);
            }
        }

        private void RebuildFormation()
        {
            float maximumFormationHalfWidth = Mathf.Min(
                config.MaximumFormationHalfWidth,
                UnitUsableTrackHalfWidth);
            FormationCalculator.Calculate(
                currentFormation,
                activeUnits.Count,
                config.RowSpacing,
                config.ColumnSpacing,
                config.SpreadSpacingMultiplier,
                config.CompactLateralSpacing,
                config.CompactDepthSpacing,
                config.MaxUnitsPerRow,
                maximumFormationHalfWidth,
                formationSlots);

            CacheIdealHorizontalBounds();

            for (int index = 0; index < activeUnits.Count; index++)
            {
                activeUnits[index].SetFormationSlot(formationSlots[index]);
            }

            UpdateRootMovementRange();

            FormationRevision++;
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDevelopmentInput();
#endif
        }

        private void CacheIdealHorizontalBounds()
        {
            if (formationSlots.Count == 0)
            {
                idealLocalMinX = 0f;
                idealLocalMaxX = 0f;
                idealLocalMaxZ = 0f;
                return;
            }

            idealLocalMinX = formationSlots[0].LocalPosition.x;
            idealLocalMaxX = idealLocalMinX;
            idealLocalMaxZ = formationSlots[0].LocalPosition.z;

            for (int index = 1; index < formationSlots.Count; index++)
            {
                float localX = formationSlots[index].LocalPosition.x;
                idealLocalMinX = Mathf.Min(idealLocalMinX, localX);
                idealLocalMaxX = Mathf.Max(idealLocalMaxX, localX);
                idealLocalMaxZ = Mathf.Max(
                    idealLocalMaxZ,
                    formationSlots[index].LocalPosition.z);
            }
        }

        private void UpdateRootMovementRange()
        {
            FormationBoundaryMath.CalculateRootHorizontalRange(
                idealLocalMinX,
                idealLocalMaxX,
                playerConfig.TrackHalfWidth,
                config.UnitBoundaryRadius,
                playerConfig.TrackBoundaryPadding,
                out float minimumRootX,
                out float maximumRootX);
            MinimumRootX = minimumRootX;
            MaximumRootX = maximumRootX;
            if (controlRoot != null)
            {
                controlRoot.SetHorizontalMovementRange(
                    MinimumRootX,
                    MaximumRootX);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDevelopmentInput()
        {
            if (!debugControlsEnabled)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Compact);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Line);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Wedge);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Column);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Spread);
            }

            int countDelta = keyboard.leftShiftKey.isPressed
                || keyboard.rightShiftKey.isPressed
                ? 5
                : 1;

            if (keyboard.equalsKey.wasPressedThisFrame)
            {
                AddUnits(countDelta);
            }

            if (keyboard.minusKey.wasPressedThisFrame)
            {
                RemoveUnits(countDelta);
            }
        }

        private void OnGUI()
        {
            if (!debugOverlayEnabled || !IsInitialized)
            {
                return;
            }

            GUI.Box(
                new Rect(12f, 12f, 310f, 78f),
                $"Squad: {CurrentUnitCount}\nFormation: {CurrentFormation}"
                + (IsDefeated
                    ? "\nDEFEATED"
                    : "\n1 Compact  2 Line  3 Wedge  4 Column  5 Spread"));
        }
#endif

        private void OnDrawGizmosSelected()
        {
            if (!drawFormationGizmos || config == null)
            {
                return;
            }

            if (!IsInitialized)
            {
                int previewUnitCount = ConfiguredStartingUnitCount;
                FormationCalculator.Calculate(
                    currentFormation,
                    previewUnitCount,
                    config.RowSpacing,
                    config.ColumnSpacing,
                    config.SpreadSpacingMultiplier,
                    config.CompactLateralSpacing,
                    config.CompactDepthSpacing,
                    config.MaxUnitsPerRow,
                    config.MaximumFormationHalfWidth,
                    formationSlots);
            }

            Transform root = formationRoot != null ? formationRoot : transform;
            Gizmos.color = new Color(0.15f, 0.9f, 1f, 0.9f);

            for (int index = 0; index < formationSlots.Count; index++)
            {
                Vector3 slotWorldPosition = root.TransformPoint(
                    formationSlots[index].LocalPosition);
                Gizmos.DrawWireSphere(
                    slotWorldPosition + (Vector3.up * 0.12f),
                    0.16f);
            }

            if (IsInitialized)
            {
                const float gizmoLength = 30f;
                float zStart = root.position.z - (gizmoLength * 0.5f);
                float zEnd = root.position.z + (gizmoLength * 0.5f);
                float usableHalfWidth = UnitUsableTrackHalfWidth;
                Gizmos.color = new Color(1f, 0.25f, 0.75f, 0.9f);
                Gizmos.DrawLine(
                    new Vector3(-usableHalfWidth, 0.08f, zStart),
                    new Vector3(-usableHalfWidth, 0.08f, zEnd));
                Gizmos.DrawLine(
                    new Vector3(usableHalfWidth, 0.08f, zStart),
                    new Vector3(usableHalfWidth, 0.08f, zEnd));
            }
        }
    }
}
