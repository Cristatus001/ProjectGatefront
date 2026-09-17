using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
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
        [SerializeField] private FormationType currentFormation = FormationType.Line;

        [Header("Development")]
        [SerializeField] private bool debugControlsEnabled = true;
        [SerializeField] private bool debugOverlayEnabled = true;
        [SerializeField] private bool drawFormationGizmos = true;

        private readonly List<SquadUnit> activeFollowers = new List<SquadUnit>();
        private readonly List<FormationSlot> formationSlots = new List<FormationSlot>();
        private Stack<SquadUnit> pooledFollowers;
        private PlayerConfig playerConfig;
        private float idealLocalMinX;
        private float idealLocalMaxX;

        public event Action<int> SquadCountChanged;

        public event Action<FormationType> FormationChanged;

        public bool IsInitialized { get; private set; }

        public int CurrentUnitCount => IsInitialized
            ? activeFollowers.Count + 1
            : 0;

        public int ActiveFollowerCount => activeFollowers.Count;

        public int FormationSlotCount => formationSlots.Count;

        public int FormationRevision { get; private set; }

        public float CurrentHorizontalCorrection { get; private set; }

        public float FollowerUsableTrackHalfWidth => playerConfig == null
            || config == null
                ? 0f
                : FormationBoundaryMath.CalculateUsableTrackHalfWidth(
                    playerConfig.TrackHalfWidth,
                    config.UnitBoundaryRadius,
                    playerConfig.TrackBoundaryPadding);

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
            Initialize(config, unitPrefab, formationRoot, trackConfig);
        }

        public void Initialize(
            SquadConfig squadConfig,
            SquadUnit squadUnitPrefab,
            Transform squadFormationRoot,
            PlayerConfig trackConfig)
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

            int maxFollowerCount = Mathf.Max(0, config.MaxSquadSize - 1);
            activeFollowers.Capacity = Mathf.Max(
                activeFollowers.Capacity,
                maxFollowerCount);
            formationSlots.Capacity = Mathf.Max(
                formationSlots.Capacity,
                maxFollowerCount);
            pooledFollowers = new Stack<SquadUnit>(maxFollowerCount);
            IsInitialized = true;

            int startingFollowerCount = ConfiguredStartingUnitCount - 1;
            Prewarm(startingFollowerCount);
            if (startingFollowerCount > 0)
            {
                AddUnits(startingFollowerCount);
            }
            else
            {
                RebuildFormation();
                SquadCountChanged?.Invoke(CurrentUnitCount);
            }
        }

        public int AddUnit()
        {
            return AddUnits(1);
        }

        public int AddUnits(int count)
        {
            if (!IsInitialized || count <= 0)
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

            Vector3 spawnLocalPosition = new Vector3(
                0f,
                0f,
                -config.RowSpacing);

            for (int index = 0; index < addCount; index++)
            {
                SquadUnit unit = AcquireFollower();
                unit.Activate(
                    formationRoot,
                    spawnLocalPosition,
                    config.FormationTransitionSpeed,
                    config.UnitRotationSpeed);
                activeFollowers.Add(unit);
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

            int removeCount = Mathf.Min(count, activeFollowers.Count);
            if (removeCount <= 0)
            {
                return 0;
            }

            for (int index = 0; index < removeCount; index++)
            {
                int lastIndex = activeFollowers.Count - 1;
                SquadUnit unit = activeFollowers[lastIndex];
                activeFollowers.RemoveAt(lastIndex);
                unit.Deactivate();
                pooledFollowers.Push(unit);
            }

            RebuildFormation();
            SquadCountChanged?.Invoke(CurrentUnitCount);
            return removeCount;
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

        public void CopyActiveFollowersTo(List<SquadUnit> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int index = 0; index < activeFollowers.Count; index++)
            {
                destination.Add(activeFollowers[index]);
            }
        }

        private void Prewarm(int count)
        {
            for (int index = 0; index < count; index++)
            {
                pooledFollowers.Push(CreatePooledFollower());
            }
        }

        private SquadUnit AcquireFollower()
        {
            return pooledFollowers.Count > 0
                ? pooledFollowers.Pop()
                : CreatePooledFollower();
        }

        private SquadUnit CreatePooledFollower()
        {
            SquadUnit unit = Instantiate(unitPrefab, formationRoot);
            unit.gameObject.SetActive(false);
            return unit;
        }

        private void RebuildFormation()
        {
            float maximumFormationHalfWidth = Mathf.Min(
                config.MaximumFormationHalfWidth,
                FollowerUsableTrackHalfWidth);
            FormationCalculator.Calculate(
                currentFormation,
                activeFollowers.Count,
                config.RowSpacing,
                config.ColumnSpacing,
                config.SpreadSpacingMultiplier,
                config.MaxUnitsPerRow,
                maximumFormationHalfWidth,
                formationSlots);

            CacheIdealHorizontalBounds();

            for (int index = 0; index < activeFollowers.Count; index++)
            {
                activeFollowers[index].SetFormationSlot(formationSlots[index]);
            }

            UpdateHorizontalContainment(true);

            FormationRevision++;
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            UpdateHorizontalContainment(false);

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
                return;
            }

            idealLocalMinX = formationSlots[0].LocalPosition.x;
            idealLocalMaxX = idealLocalMinX;

            for (int index = 1; index < formationSlots.Count; index++)
            {
                float localX = formationSlots[index].LocalPosition.x;
                idealLocalMinX = Mathf.Min(idealLocalMinX, localX);
                idealLocalMaxX = Mathf.Max(idealLocalMaxX, localX);
            }
        }

        private void UpdateHorizontalContainment(bool force)
        {
            float correction = FormationBoundaryMath.CalculateHorizontalFormationCorrection(
                activeFollowers.Count,
                formationRoot.position.x,
                idealLocalMinX,
                idealLocalMaxX,
                playerConfig.TrackHalfWidth,
                config.UnitBoundaryRadius,
                playerConfig.TrackBoundaryPadding);

            if (!force && Mathf.Approximately(correction, CurrentHorizontalCorrection))
            {
                return;
            }

            CurrentHorizontalCorrection = correction;
            for (int index = 0; index < activeFollowers.Count; index++)
            {
                activeFollowers[index].SetHorizontalWorldCorrection(correction);
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
                SetFormation(FormationType.Line);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Wedge);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SetFormation(FormationType.Column);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
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
                new Rect(12f, 12f, 190f, 58f),
                $"Squad: {CurrentUnitCount}\nFormation: {CurrentFormation}");
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
                int previewFollowerCount = Mathf.Max(
                    0,
                    ConfiguredStartingUnitCount - 1);
                FormationCalculator.Calculate(
                    currentFormation,
                    previewFollowerCount,
                    config.RowSpacing,
                    config.ColumnSpacing,
                    config.SpreadSpacingMultiplier,
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
                slotWorldPosition.x += CurrentHorizontalCorrection;
                Gizmos.DrawWireSphere(
                    slotWorldPosition + (Vector3.up * 0.12f),
                    0.16f);
            }

            if (IsInitialized)
            {
                const float gizmoLength = 30f;
                float zStart = root.position.z - (gizmoLength * 0.5f);
                float zEnd = root.position.z + (gizmoLength * 0.5f);
                float usableHalfWidth = FollowerUsableTrackHalfWidth;
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
