using System;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Player
{
    [DisallowMultipleComponent]
    public sealed class RunnerCameraController : MonoBehaviour
    {
        [Tooltip("World-space camera offset from the camera target.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 7.5f, -10f);

        [Tooltip("Time used to smooth camera position changes.")]
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.16f;

        [Tooltip("Distance ahead of the player used to establish the fixed runner viewing angle.")]
        [SerializeField, Min(0f)] private float lookAheadDistance = 7.5f;

        [SerializeField] private Transform followTarget;
        private Vector3 followVelocity;
        private bool followEnabled;

        public bool IsInitialized => followTarget != null;

        private void LateUpdate()
        {
            if (!followEnabled || followTarget == null)
            {
                return;
            }

            Vector3 desiredPosition = followTarget.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.deltaTime);
        }

        public void Initialize(Transform target)
        {
            followTarget = target != null
                ? target
                : throw new ArgumentNullException(nameof(target));

            followVelocity = Vector3.zero;
            transform.position = followTarget.position + offset;
            ApplyFixedRotation();
            followEnabled = true;
        }

        public void SetFollowEnabled(bool enabled)
        {
            followEnabled = enabled;
            followVelocity = Vector3.zero;
        }

        private void ApplyFixedRotation()
        {
            Vector3 viewDirection = (Vector3.forward * lookAheadDistance) - offset;
            if (viewDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(viewDirection, Vector3.up);
            }
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
            Gizmos.DrawWireSphere(followTarget.position, 0.2f);
            Gizmos.DrawLine(transform.position, followTarget.position);
        }
    }
}
