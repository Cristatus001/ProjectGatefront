using System;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Player
{
    [DisallowMultipleComponent]
    public sealed class RunnerCameraController : MonoBehaviour
    {
        [Tooltip("Stable world-space camera position for portrait combat framing.")]
        [SerializeField] private Vector3 fixedPosition = new Vector3(0f, 8.85f, -7.6f);

        [Tooltip("World-space offset from the combat anchor used as the fixed look point.")]
        [SerializeField] private Vector3 lookTargetOffset = new Vector3(0f, 0f, 9.5f);

        [Tooltip("Fraction of player horizontal movement applied to the camera position.")]
        [SerializeField, Range(0f, 1f)] private float horizontalFollowAmount;

        [Tooltip("Time used to smooth the optional horizontal camera follow.")]
        [SerializeField, Min(0.01f)] private float horizontalSmoothTime = 0.16f;

        [SerializeField] private Transform followTarget;
        private float horizontalVelocity;
        private bool followEnabled;

        public bool IsInitialized => followTarget != null;

        private void LateUpdate()
        {
            if (!followEnabled || followTarget == null)
            {
                return;
            }

            float desiredX = fixedPosition.x
                + (followTarget.position.x * horizontalFollowAmount);
            float smoothedX = Mathf.SmoothDamp(
                transform.position.x,
                desiredX,
                ref horizontalVelocity,
                horizontalSmoothTime,
                Mathf.Infinity,
                Time.deltaTime);
            transform.position = new Vector3(
                smoothedX,
                fixedPosition.y,
                fixedPosition.z);
        }

        public void Initialize(Transform target)
        {
            followTarget = target != null
                ? target
                : throw new ArgumentNullException(nameof(target));

            horizontalVelocity = 0f;
            transform.position = fixedPosition;
            ApplyFixedRotation();
            followEnabled = true;
        }

        public void SetFollowEnabled(bool enabled)
        {
            followEnabled = enabled;
            horizontalVelocity = 0f;
        }

        private void ApplyFixedRotation()
        {
            Vector3 anchorPosition = followTarget == null
                ? new Vector3(0f, 1.35f, 2f)
                : followTarget.position;
            anchorPosition.x = 0f;
            Vector3 viewDirection = anchorPosition + lookTargetOffset - fixedPosition;
            if (viewDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(viewDirection, Vector3.up);
            }
        }

        public void ConfigureFixedPresentation(
            Vector3 position,
            Vector3 targetOffset,
            float followAmount,
            float smoothTime)
        {
            fixedPosition = position;
            lookTargetOffset = targetOffset;
            horizontalFollowAmount = Mathf.Clamp01(followAmount);
            horizontalSmoothTime = Mathf.Max(0.01f, smoothTime);
        }

        private void OnValidate()
        {
            ApplyFixedRotation();
        }

        private void OnDrawGizmosSelected()
        {
            if (followTarget == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Vector3 lookPoint = followTarget.position + lookTargetOffset;
            lookPoint.x = 0f;
            Gizmos.DrawWireSphere(lookPoint, 0.2f);
            Gizmos.DrawLine(transform.position, lookPoint);
        }
    }
}
