using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    public static class EnemyContactMath
    {
        public static bool HasReachedContactLine(
            float enemyCenterZ,
            float enemyRadius,
            float contactLineZ)
        {
            return enemyCenterZ - Mathf.Max(0f, enemyRadius) <= contactLineZ;
        }

        public static bool OverlapsSquadHorizontally(
            float enemyCenterX,
            float enemyRadius,
            float squadMinimumX,
            float squadMaximumX,
            float contactPadding)
        {
            float combinedRadius = Mathf.Max(0f, enemyRadius)
                + Mathf.Max(0f, contactPadding);
            float enemyMinimumX = enemyCenterX - combinedRadius;
            float enemyMaximumX = enemyCenterX + combinedRadius;
            float minimumX = Mathf.Min(squadMinimumX, squadMaximumX);
            float maximumX = Mathf.Max(squadMinimumX, squadMaximumX);
            return enemyMaximumX >= minimumX && enemyMinimumX <= maximumX;
        }
    }
}
