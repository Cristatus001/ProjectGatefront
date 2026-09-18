using UnityEngine;

namespace ProjectGatefront.Gameplay.World
{
    [DisallowMultipleComponent]
    public sealed class ScrollItem : MonoBehaviour
    {
        [SerializeField] private bool recyclable;
        [SerializeField, Min(0f)] private float recycleLength = 30f;

        private Vector3 initialWorldPosition;
        private bool hasInitialState;

        public bool IsRecyclable => recyclable;

        public float RecycleLength => recycleLength;

        public float WorldZ => transform.position.z;

        public void Configure(bool shouldRecycle, float itemLength)
        {
            recyclable = shouldRecycle;
            recycleLength = Mathf.Max(0f, itemLength);
        }

        public void CaptureInitialState()
        {
            initialWorldPosition = transform.position;
            hasInitialState = true;
        }

        public void AdvanceTowardsPlayer(float distance)
        {
            float safeDistance = WorldScrollMath.SanitizeSpeed(distance);
            if (safeDistance <= 0f)
            {
                return;
            }

            Vector3 position = transform.position;
            position.z -= safeDistance;
            transform.position = position;
        }

        public bool ShouldRecycle(float recycleBehindZ)
        {
            return recyclable && transform.position.z <= recycleBehindZ;
        }

        public void RecycleTo(float targetWorldZ)
        {
            Vector3 position = transform.position;
            position.z = targetWorldZ;
            transform.position = position;
        }

        public void ResetToInitialState()
        {
            if (hasInitialState)
            {
                transform.position = initialWorldPosition;
            }
        }
    }
}
