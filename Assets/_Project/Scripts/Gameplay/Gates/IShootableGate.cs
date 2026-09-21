using ProjectGatefront.Gameplay.Combat;

namespace ProjectGatefront.Gameplay.Gates
{
    public interface IShootableGate : ITargetable
    {
        bool CanReceiveFire(float squadX, float squadContactZ);

        bool ApplyProjectileHit();
    }
}
