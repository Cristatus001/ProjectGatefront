using UnityEngine;

namespace ProjectGatefront.Gameplay.Combat
{
    public interface ITargetable
    {
        Transform TargetTransform { get; }

        Vector3 AimPosition { get; }

        float TargetRadius { get; }

        bool IsTargetable { get; }
    }
}
