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
        private bool movementEnabled;

        public bool IsInitialized { get; private set; }

        public float CurrentHorizontalPosition => transform.position.x;

        public float TrackHalfWidth => config == null ? 0f : config.TrackHalfWidth;

        public float TrackBoundaryPadding => config == null
            ? 0f
            : config.TrackBoundaryPadding;

        public float LeaderUsableTrackHalfWidth => config == null
            ? 0f
            : PlayerMovementMath.CalculateUsableTrackHalfWidth(
                config.TrackHalfWidth,
                config.LeaderBoundaryRadius,
                config.TrackBoundaryPadding);

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
                    LeaderUsableTrackHalfWidth);
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
                LeaderUsableTrackHalfWidth);

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
            lateralTarget = PlayerMovementMath.ClampHorizontal(
                transform.position.x,
                LeaderUsableTrackHalfWidth);
            lateralVelocity = 0f;
            IsInitialized = true;
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
                new Vector3(-LeaderUsableTrackHalfWidth, 0.06f, zStart),
                new Vector3(-LeaderUsableTrackHalfWidth, 0.06f, zEnd));
            Gizmos.DrawLine(
                new Vector3(LeaderUsableTrackHalfWidth, 0.06f, zStart),
                new Vector3(LeaderUsableTrackHalfWidth, 0.06f, zEnd));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                new Vector3(0f, 0.05f, zStart),
                new Vector3(0f, 0.05f, zEnd));
        }
    }
}
