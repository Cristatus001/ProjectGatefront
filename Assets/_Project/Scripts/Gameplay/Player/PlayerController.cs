using System;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Development")]
        [Tooltip("Draw the configured track limits while this object is selected.")]
        [SerializeField] private bool drawBoundaryGizmos = true;

        private CharacterController characterController;
        [SerializeField] private PlayerConfig config;
        private IPlayerInputSource inputSource;
        private float lateralTarget;
        private float lateralVelocity;
        private float minimumHorizontalPosition;
        private float maximumHorizontalPosition;
        private bool movementEnabled;

        public bool IsInitialized { get; private set; }

        public float CurrentHorizontalPosition => transform.position.x;

        public float TrackHalfWidth => config == null ? 0f : config.TrackHalfWidth;

        public float TrackBoundaryPadding => config == null
            ? 0f
            : config.TrackBoundaryPadding;

        public float DefaultUsableTrackHalfWidth => config == null
            ? 0f
            : Mathf.Max(0f, config.TrackHalfWidth - config.TrackBoundaryPadding);

        public float MinimumHorizontalPosition => minimumHorizontalPosition;

        public float MaximumHorizontalPosition => maximumHorizontalPosition;

        public float CombatAnchorZ => config == null ? transform.position.z : config.CombatAnchorZ;

        public float CurrentDragDelta { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            lateralTarget = transform.position.x;
        }

        private void Update()
        {
            if (!IsInitialized || !movementEnabled)
            {
                CurrentDragDelta = 0f;
                return;
            }

            PlayerInputFrame inputFrame = inputSource.ReadFrame();
            CurrentDragDelta = inputFrame.PointerHeld ? inputFrame.HorizontalDelta : 0f;

            if (inputFrame.PointerUp)
            {
                lateralTarget = transform.position.x;
                lateralVelocity = 0f;
            }
            else if (inputFrame.PointerHeld)
            {
                lateralTarget = PlayerMovementMath.CalculateLateralTarget(
                    lateralTarget,
                    CurrentDragDelta,
                    config.LateralSensitivity,
                    minimumHorizontalPosition,
                    maximumHorizontalPosition);
            }

            float nextHorizontal = Mathf.SmoothDamp(
                transform.position.x,
                lateralTarget,
                ref lateralVelocity,
                config.LateralSmoothTime,
                config.MaximumLateralSpeed,
                Time.deltaTime);

            nextHorizontal = PlayerMovementMath.ClampHorizontal(
                nextHorizontal,
                minimumHorizontalPosition,
                maximumHorizontalPosition);

            Vector3 motion = new Vector3(
                nextHorizontal - transform.position.x,
                0f,
                config.CombatAnchorZ - transform.position.z);

            characterController.Move(motion);
        }

        public void Initialize(PlayerConfig playerConfig, IPlayerInputSource playerInputSource)
        {
            config = playerConfig ?? throw new ArgumentNullException(nameof(playerConfig));
            inputSource = playerInputSource ?? throw new ArgumentNullException(nameof(playerInputSource));
            Vector3 anchoredPosition = transform.position;
            anchoredPosition.z = config.CombatAnchorZ;
            transform.position = anchoredPosition;
            minimumHorizontalPosition = -DefaultUsableTrackHalfWidth;
            maximumHorizontalPosition = DefaultUsableTrackHalfWidth;
            lateralTarget = PlayerMovementMath.ClampHorizontal(
                transform.position.x,
                minimumHorizontalPosition,
                maximumHorizontalPosition);
            lateralVelocity = 0f;
            IsInitialized = true;
        }

        public void SetHorizontalMovementRange(
            float minimumHorizontal,
            float maximumHorizontal)
        {
            minimumHorizontalPosition = Mathf.Min(
                minimumHorizontal,
                maximumHorizontal);
            maximumHorizontalPosition = Mathf.Max(
                minimumHorizontal,
                maximumHorizontal);
            lateralTarget = PlayerMovementMath.ClampHorizontal(
                lateralTarget,
                minimumHorizontalPosition,
                maximumHorizontalPosition);

            float clampedPosition = PlayerMovementMath.ClampHorizontal(
                transform.position.x,
                minimumHorizontalPosition,
                maximumHorizontalPosition);
            float horizontalDelta = clampedPosition - transform.position.x;
            if (Mathf.Abs(horizontalDelta) <= Mathf.Epsilon)
            {
                return;
            }

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(new Vector3(horizontalDelta, 0f, 0f));
            }
            else
            {
                Vector3 position = transform.position;
                position.x = clampedPosition;
                transform.position = position;
            }

            lateralVelocity = 0f;
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            lateralTarget = transform.position.x;
            lateralVelocity = 0f;
            CurrentDragDelta = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawBoundaryGizmos || config == null)
            {
                return;
            }

            const float gizmoLength = 30f;
            float zStart = transform.position.z - (gizmoLength * 0.5f);
            float zEnd = transform.position.z + (gizmoLength * 0.5f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(-config.TrackHalfWidth, 0.05f, zStart),
                new Vector3(-config.TrackHalfWidth, 0.05f, zEnd));
            Gizmos.DrawLine(
                new Vector3(config.TrackHalfWidth, 0.05f, zStart),
                new Vector3(config.TrackHalfWidth, 0.05f, zEnd));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                new Vector3(minimumHorizontalPosition, 0.06f, zStart),
                new Vector3(minimumHorizontalPosition, 0.06f, zEnd));
            Gizmos.DrawLine(
                new Vector3(maximumHorizontalPosition, 0.06f, zStart),
                new Vector3(maximumHorizontalPosition, 0.06f, zEnd));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                new Vector3(0f, 0.05f, zStart),
                new Vector3(0f, 0.05f, zEnd));
        }
    }
}
