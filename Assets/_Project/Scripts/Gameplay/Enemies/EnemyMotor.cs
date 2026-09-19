using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyMotor : MonoBehaviour
    {
        private float approachSpeed;

        public float ApproachSpeed => approachSpeed;

        public void Configure(float speed)
        {
            approachSpeed = !float.IsNaN(speed) && !float.IsInfinity(speed)
                ? Mathf.Max(0f, speed)
                : 0f;
        }

        public float Advance(
            float worldScrollSpeed,
            float deltaTime,
            bool movementEnabled)
        {
            float distance = EnemyMovementMath.CalculateApproachDistance(
                worldScrollSpeed,
                approachSpeed,
                deltaTime,
                movementEnabled);
            if (distance > 0f)
            {
                transform.position += Vector3.back * distance;
            }

            return distance;
        }
    }
}
