using System;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Gates
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldContentCarrier))]
    public sealed class GateController : MonoBehaviour, IShootableGate
    {
        private const float FeedbackDuration = 0.12f;
        private static readonly string[] SignedLabels = BuildSignedLabels();

        [SerializeField] private Transform panel;
        [SerializeField] private Transform leftPost;
        [SerializeField] private Transform rightPost;
        [SerializeField] private Transform topBeam;
        [SerializeField] private MeshRenderer panelRenderer;
        [SerializeField] private TextMesh valueLabel;
        [SerializeField] private Material negativeMaterial;
        [SerializeField] private Material neutralMaterial;
        [SerializeField] private Material positiveMaterial;
        [SerializeField] private Material divideMaterial;

        private WorldContentCarrier carrier;
        private TargetRegistry registry;
        private Vector3 labelBaseScale = Vector3.one;
        private float halfWidth;
        private float engagementRange;
        private float feedbackRemaining;
        private bool milestoneFeedback;

        public event Action<int, int> GateValueChanged;

        public GateKind Kind { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsLocked { get; private set; }
        public int StartingValue { get; private set; }
        public int CurrentValue { get; private set; }
        public int MinimumValue { get; private set; }
        public int MaximumValue { get; private set; }
        public int DivideAmount { get; private set; }
        public float HalfWidth => halfWidth;
        public float WorldZ => transform.position.z;
        public float WorldX => transform.position.x;
        public Transform TargetTransform => transform;
        public Vector3 AimPosition => transform.position + (Vector3.up * 1.55f);
        public float TargetRadius => Mathf.Max(0.5f, halfWidth);
        public bool IsTargetable => IsActive
            && !IsLocked
            && Kind == GateKind.ShootableAdditive
            && gameObject.activeInHierarchy;

        private void Awake()
        {
            carrier = GetComponent<WorldContentCarrier>();
            if (valueLabel != null)
            {
                labelBaseScale = valueLabel.transform.localScale;
            }
        }

        private void Update()
        {
            TickFeedback(Time.deltaTime);
        }

        public void ConfigureVisuals(
            Transform gatePanel,
            Transform leftFramePost,
            Transform rightFramePost,
            Transform frameTop,
            MeshRenderer gatePanelRenderer,
            TextMesh label,
            Material negative,
            Material neutral,
            Material positive,
            Material divide)
        {
            panel = gatePanel;
            leftPost = leftFramePost;
            rightPost = rightFramePost;
            topBeam = frameTop;
            panelRenderer = gatePanelRenderer;
            valueLabel = label;
            negativeMaterial = negative;
            neutralMaterial = neutral;
            positiveMaterial = positive;
            divideMaterial = divide;
            labelBaseScale = label == null
                ? Vector3.one
                : label.transform.localScale;
        }

        public void Activate(
            GateEncounterDefinition encounter,
            GateKind gateKind,
            WorldScroller scroller,
            TargetRegistry targetRegistry,
            Vector3 worldPosition,
            float width,
            float passWorldZ)
        {
            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            if (targetRegistry == null)
            {
                throw new ArgumentNullException(nameof(targetRegistry));
            }

            Deactivate();
            carrier = carrier != null ? carrier : GetComponent<WorldContentCarrier>();
            Kind = gateKind;
            registry = targetRegistry;
            StartingValue = encounter.ShootableStartingValue;
            MinimumValue = encounter.ShootableMinimumValue;
            MaximumValue = encounter.ShootableMaximumValue;
            CurrentValue = StartingValue;
            DivideAmount = encounter.DivideAmount;
            halfWidth = Mathf.Max(0.5f, width * 0.5f);
            engagementRange = encounter.EngagementRange;
            IsLocked = false;
            feedbackRemaining = 0f;
            milestoneFeedback = false;
            if (valueLabel != null)
            {
                valueLabel.transform.localScale = labelBaseScale;
            }
            ConfigureWidth(width);
            carrier.Activate(scroller, worldPosition, passWorldZ);
            IsActive = true;
            if (Kind == GateKind.ShootableAdditive)
            {
                registry.Register(this);
            }

            RefreshVisual();
        }

        public bool CanReceiveFire(float squadX, float squadContactZ)
        {
            return IsTargetable
                && CurrentValue < MaximumValue
                && GateMath.IsAligned(squadX, WorldX, halfWidth + 0.15f)
                && WorldZ > squadContactZ
                && WorldZ - squadContactZ <= engagementRange;
        }

        public bool ApplyProjectileHit()
        {
            if (!IsTargetable || CurrentValue >= MaximumValue)
            {
                return false;
            }

            int previous = CurrentValue;
            CurrentValue = GateMath.Charge(CurrentValue, MaximumValue);
            milestoneFeedback = (previous < 0 && CurrentValue == 0)
                || (previous == 0 && CurrentValue > 0)
                || CurrentValue == MaximumValue;
            feedbackRemaining = FeedbackDuration;
            RefreshVisual();
            GateValueChanged?.Invoke(previous, CurrentValue);
            return true;
        }

        public int LockValue()
        {
            if (!IsLocked)
            {
                IsLocked = true;
                registry?.Unregister(this);
            }

            return CurrentValue;
        }

        public void Deactivate()
        {
            registry?.Unregister(this);
            IsActive = false;
            IsLocked = false;
            feedbackRemaining = 0f;
            if (carrier != null)
            {
                carrier.Deactivate();
            }

            registry = null;
        }

        private void ConfigureWidth(float width)
        {
            float safeWidth = Mathf.Max(1f, width);
            if (panel != null)
            {
                panel.localPosition = new Vector3(0f, 1.55f, 0f);
                panel.localScale = new Vector3(safeWidth, 2.55f, 0.18f);
            }

            if (leftPost != null)
            {
                leftPost.localPosition = new Vector3(-safeWidth * 0.5f, 1.55f, 0f);
                leftPost.localScale = new Vector3(0.18f, 3.15f, 0.3f);
            }

            if (rightPost != null)
            {
                rightPost.localPosition = new Vector3(safeWidth * 0.5f, 1.55f, 0f);
                rightPost.localScale = new Vector3(0.18f, 3.15f, 0.3f);
            }

            if (topBeam != null)
            {
                topBeam.localPosition = new Vector3(0f, 3.1f, 0f);
                topBeam.localScale = new Vector3(safeWidth + 0.2f, 0.18f, 0.3f);
            }
        }

        private void RefreshVisual()
        {
            if (valueLabel != null)
            {
                valueLabel.text = Kind == GateKind.Divide
                    ? "÷" + DivideAmount
                    : SignedLabels[Mathf.Clamp(CurrentValue, -999, 999) + 999];
                valueLabel.color = milestoneFeedback
                    ? new Color(1f, 0.95f, 0.45f)
                    : Kind == GateKind.Divide
                        ? new Color(0.74f, 0.9f, 1f)
                        : CurrentValue < 0
                            ? new Color(1f, 0.76f, 0.74f)
                            : CurrentValue == 0
                                ? Color.white
                                : new Color(0.68f, 1f, 0.7f);
            }

            if (panelRenderer != null)
            {
                panelRenderer.sharedMaterial = Kind == GateKind.Divide
                    ? divideMaterial
                    : CurrentValue < 0
                        ? negativeMaterial
                        : CurrentValue == 0
                            ? neutralMaterial
                            : positiveMaterial;
            }
        }

        private void TickFeedback(float deltaTime)
        {
            if (!IsActive || valueLabel == null || feedbackRemaining <= 0f)
            {
                return;
            }

            feedbackRemaining = Mathf.Max(0f, feedbackRemaining - deltaTime);
            float normalized = feedbackRemaining / FeedbackDuration;
            valueLabel.transform.localScale = labelBaseScale
                * (1f + (normalized * (milestoneFeedback ? 0.3f : 0.12f)));
            if (feedbackRemaining <= 0f)
            {
                milestoneFeedback = false;
                valueLabel.transform.localScale = labelBaseScale;
                RefreshVisual();
            }
        }

        private void OnDisable()
        {
            registry?.Unregister(this);
            IsActive = false;
        }

        private static string[] BuildSignedLabels()
        {
            var labels = new string[1999];
            for (int value = -999; value <= 999; value++)
            {
                labels[value + 999] = value > 0
                    ? "+" + value
                    : value.ToString();
            }

            return labels;
        }
    }
}
