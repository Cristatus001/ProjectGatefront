using UnityEngine;

namespace ProjectGatefront.Gameplay.Combat
{
    public interface IProjectileHitVolume
    {
        bool TryGetProjectileIntersection(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float projectileRadius,
            out float normalizedDistance);
    }
}
