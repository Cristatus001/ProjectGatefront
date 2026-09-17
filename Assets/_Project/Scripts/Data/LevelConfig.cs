using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Level", fileName = "LevelConfig")]
    public sealed class LevelConfig : GameConfig
    {
        [SerializeField, Min(1)] private int missionNumber = 1;
        [SerializeField] private string sceneName = "02_Gameplay";
        [SerializeField] private BossConfig finalBoss;

        public int MissionNumber => missionNumber;

        public string SceneName => sceneName;

        public BossConfig FinalBoss => finalBoss;
    }
}
