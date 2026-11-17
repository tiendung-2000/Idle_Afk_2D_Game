using UnityEngine;

namespace PixelImmortal.Data
{
    public enum QuestObjective
    {
        KillEnemies,
        EarnLinhThach,
        EarnLinhKhi,
        UpgradeTimes,
        CompleteWaves,
        DefeatBoss
    }

    /// <summary>A daily quest. Title key = "quest." + id; description key = "quest.objective." + objective.</summary>
    [CreateAssetMenu(fileName = "Quest_", menuName = "PixelImmortal/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used by saves and localization keys.")]
        public string id;
        public QuestObjective objective;
        public double targetCount = 10;
        public RewardEntry reward = new RewardEntry();
    }
}
