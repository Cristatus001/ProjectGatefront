using UnityEngine;

namespace ProjectGatefront.Gameplay.Squad
{
    public readonly struct FormationSlot
    {
        public FormationSlot(int index, Vector3 localPosition)
        {
            Index = index;
            LocalPosition = localPosition;
        }

        public int Index { get; }

        public Vector3 LocalPosition { get; }
    }
}
