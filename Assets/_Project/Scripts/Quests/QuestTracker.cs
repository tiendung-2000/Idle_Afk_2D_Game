using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Enemies;
using PixelImmortal.Progression;
using PixelImmortal.Upgrades;
using UnityEngine;

namespace PixelImmortal.Quests
{
    /// <summary>Feeds live-play events into the quest system and checks the day rollover once a second.</summary>
    public class QuestTracker : MonoBehaviour
    {
        QuestSystem quests;
        BattleDirector director;
        Wallet wallet;
        UpgradeSystem upgrades;
        float nextTick;

        public void Init(QuestSystem questSystem, BattleDirector dir, Wallet w, UpgradeSystem up)
        {
            quests = questSystem;
            director = dir;
            wallet = w;
            upgrades = up;
            director.EnemyKilled += OnKilled;
            director.WaveCleared += OnWaveCleared;
            director.BossDefeatedEvent += OnBoss;
            wallet.Gained += OnGained;
            upgrades.Purchased += OnPurchased;
        }

        void OnDestroy()
        {
            if (director != null)
            {
                director.EnemyKilled -= OnKilled;
                director.WaveCleared -= OnWaveCleared;
                director.BossDefeatedEvent -= OnBoss;
            }
            if (wallet != null) wallet.Gained -= OnGained;
            if (upgrades != null) upgrades.Purchased -= OnPurchased;
        }

        void OnKilled(EnemyUnit e) { if (!e.IsBoss) quests.AddProgress(QuestObjective.KillEnemies, 1); }
        void OnWaveCleared() => quests.AddProgress(QuestObjective.CompleteWaves, 1);
        void OnBoss() => quests.AddProgress(QuestObjective.DefeatBoss, 1);
        void OnPurchased() => quests.AddProgress(QuestObjective.UpgradeTimes, 1);

        void OnGained(ResourceType t, double amount)
        {
            if (t == ResourceType.LinhThach) quests.AddProgress(QuestObjective.EarnLinhThach, amount);
            else if (t == ResourceType.LinhKhi) quests.AddProgress(QuestObjective.EarnLinhKhi, amount);
        }

        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 1f;
            quests.EnsureCurrentDay();
        }
    }
}
