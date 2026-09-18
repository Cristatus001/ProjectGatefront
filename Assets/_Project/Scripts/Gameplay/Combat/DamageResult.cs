namespace ProjectGatefront.Gameplay.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(
            float appliedDamage,
            float remainingHealth,
            bool wasKilled)
        {
            AppliedDamage = appliedDamage;
            RemainingHealth = remainingHealth;
            WasKilled = wasKilled;
        }

        public float AppliedDamage { get; }

        public float RemainingHealth { get; }

        public bool WasKilled { get; }
    }
}
