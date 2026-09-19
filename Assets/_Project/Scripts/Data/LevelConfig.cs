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
        [SerializeField] private List<EnemyWaveDefinition> enemyWaves =
            new List<EnemyWaveDefinition>();
        [SerializeField, Range(1, 200)] private int activeEnemyLimit = 100;

        public int MissionNumber => missionNumber;

        public string SceneName => sceneName;

        public BossConfig FinalBoss => finalBoss;

        public IReadOnlyList<EnemyWaveDefinition> EnemyWaves => enemyWaves;

        public int ActiveEnemyLimit => Mathf.Clamp(activeEnemyLimit, 1, 200);

        public void ConfigureEnemyWaves(
            IList<EnemyWaveDefinition> waves,
            int maximumActiveEnemies)
        {
            enemyWaves.Clear();
            if (waves != null)
            {
                for (int index = 0; index < waves.Count; index++)
                {
                    if (waves[index] != null)
                    {
                        enemyWaves.Add(waves[index]);
                    }
                }
            }

            activeEnemyLimit = Mathf.Clamp(maximumActiveEnemies, 1, 200);
        }
    }
}
