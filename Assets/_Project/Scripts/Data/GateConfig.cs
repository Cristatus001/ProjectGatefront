using UnityEngine;

namespace ProjectGatefront.Data
{
    public enum GateEffectType
    {
        SquadSize,
        Weapon,
        FireRate,
        Damage,
        Formation,
        SpecialistChance,
        Armor,
        RiskReward,
        ShootableAdditive,
        Divide
    }

    [CreateAssetMenu(menuName = "Project Gatefront/Config/Gate", fileName = "GateConfig")]
    public sealed class GateConfig : GameConfig
    {
        [SerializeField] private string displayLabel = "Gate";
        [SerializeField] private GateEffectType effectType;
        [SerializeField] private float effectValue;

        public string DisplayLabel => displayLabel;

        public GateEffectType EffectType => effectType;

        public float EffectValue => effectValue;
    }
}
