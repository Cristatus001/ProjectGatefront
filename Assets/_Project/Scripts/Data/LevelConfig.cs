using System.Collections.Generic;
using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Level", fileName = "LevelConfig")]
    public sealed class LevelConfig : GameConfig
    {
        [SerializeField, Min(1)] private int missionNumber = 1;
        [SerializeField] private string sceneName = "02_Gameplay";
        [SerializeField] private BossConfig finalBoss;
        [SerializeField] private EnemyConfig normalEnemy;
        [SerializeField] private List<CombatSectionDefinition> combatSections =
            new List<CombatSectionDefinition>();
        [SerializeField, Range(1, 200)] private int activeEnemyLimit = 100;
        [SerializeField, Min(0f)] private float bossDistance = 520f;
        [SerializeField] private List<GateEncounterDefinition> gateEncounters =
            new List<GateEncounterDefinition>();

        public int MissionNumber => missionNumber;

        public string SceneName => sceneName;

        public BossConfig FinalBoss => finalBoss;

        public EnemyConfig NormalEnemy => normalEnemy;

        public IReadOnlyList<CombatSectionDefinition> CombatSections => combatSections;

        public int ActiveEnemyLimit => Mathf.Clamp(activeEnemyLimit, 1, 200);

        public float BossDistance => Mathf.Max(0f, bossDistance);

        public IReadOnlyList<GateEncounterDefinition> GateEncounters => gateEncounters;

        public void ConfigureCombat(
            EnemyConfig enemy,
            IList<CombatSectionDefinition> sections,
            int maximumActiveEnemies,
            float giantTriggerDistance)
        {
            normalEnemy = enemy;
            combatSections.Clear();
            if (sections != null)
            {
                for (int index = 0; index < sections.Count; index++)
                {
                    if (sections[index] != null)
                    {
                        combatSections.Add(sections[index]);
                    }
                }
            }

            activeEnemyLimit = Mathf.Clamp(maximumActiveEnemies, 1, 200);
            bossDistance = Mathf.Max(0f, giantTriggerDistance);
        }

        public void ConfigureGateEncounters(IList<GateEncounterDefinition> encounters)
        {
            gateEncounters.Clear();
            if (encounters == null)
            {
                return;
            }

            for (int index = 0; index < encounters.Count; index++)
            {
                if (encounters[index] != null)
                {
                    gateEncounters.Add(encounters[index]);
                }
            }
        }
    }
}
