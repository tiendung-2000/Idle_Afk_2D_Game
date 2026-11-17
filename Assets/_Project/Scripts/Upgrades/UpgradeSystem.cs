using PixelImmortal.Data;
using PixelImmortal.Economy;

namespace PixelImmortal.Upgrades
{
    /// <summary>Business rules for buying upgrades and skill levels. UI only calls these; never trusts button state.</summary>
    public class UpgradeSystem
    {
        readonly PlayerProgress progress;

        public UpgradeSystem(PlayerProgress progress) { this.progress = progress; }

        /// <summary>Raised after a successful upgrade or skill level purchase.</summary>
        public event System.Action Purchased;

        public bool IsUnlocked(UpgradeDefinition d) => progress.RealmIndex >= d.unlockRealmIndex;

        public bool IsMaxed(UpgradeDefinition d) => d.maxLevel > 0 && progress.GetUpgradeLevel(d.id) >= d.maxLevel;

        public double Cost(UpgradeDefinition d) => UpgradeMath.Cost(d.baseCost, d.growthRate, progress.GetUpgradeLevel(d.id));

        public bool CanBuy(UpgradeDefinition d)
            => d != null && IsUnlocked(d) && !IsMaxed(d) && progress.Wallet.CanAfford(d.costResource, Cost(d));

        public bool TryBuy(UpgradeDefinition d)
        {
            if (!CanBuy(d)) return false;
            if (!progress.Wallet.TrySpend(d.costResource, Cost(d))) return false;
            progress.SetUpgradeLevel(d.id, progress.GetUpgradeLevel(d.id) + 1);
            Purchased?.Invoke();
            return true;
        }

        // ---- skills (Tinh Hoa) -----------------------------------------------

        public bool IsSkillMaxed(SkillDefinition s) => progress.GetSkillLevel(s.id) >= s.maxLevel;

        /// <summary>Cost to go from level L to L+1 = base * growth^(L-1).</summary>
        public double SkillCost(SkillDefinition s) => UpgradeMath.Cost(s.upgradeBaseCost, s.upgradeGrowthRate, progress.GetSkillLevel(s.id) - 1);

        public bool CanUpgradeSkill(SkillDefinition s)
            => s != null && progress.IsSkillUnlocked(s) && !IsSkillMaxed(s)
               && progress.Wallet.CanAfford(ResourceType.TinhHoa, SkillCost(s));

        public bool TryUpgradeSkill(SkillDefinition s)
        {
            if (!CanUpgradeSkill(s)) return false;
            if (!progress.Wallet.TrySpend(ResourceType.TinhHoa, SkillCost(s))) return false;
            progress.SetSkillLevel(s.id, progress.GetSkillLevel(s.id) + 1);
            Purchased?.Invoke();
            return true;
        }

        public float SkillMultiplier(SkillDefinition s) => SkillMultiplierAt(s, progress.GetSkillLevel(s.id));

        public static float SkillMultiplierAt(SkillDefinition s, int level)
            => s.baseMultiplier * (1f + s.perLevelBonus * (level - 1));
    }
}
