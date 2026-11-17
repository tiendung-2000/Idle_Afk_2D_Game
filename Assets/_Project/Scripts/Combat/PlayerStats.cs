using System;
using PixelImmortal.Data;
using PixelImmortal.Economy;

namespace PixelImmortal.Combat
{
    public class PlayerStats
    {
        public float Attack;
        public float MaxHp;
        public float AttackInterval;
        public float CritChance;
        public float CritMultiplier;
        public float SkillDamageMultiplier;
        public float DamageReduction;
        public float OfflineEfficiency;

        /// <summary>Base config x realm multipliers x upgrade effects. Pure function of its inputs.</summary>
        public static PlayerStats Compute(PrototypeConfig cfg, EconomyConfig eco, PlayerProgress p)
        {
            float atkBonus = 0, hpBonus = 0, speedBonus = 0, skillBonus = 0, offlineBonus = 0, critBonus = 0;
            if (eco.upgrades != null)
            {
                foreach (var u in eco.upgrades)
                {
                    if (u == null) continue;
                    float v = u.effectPerLevel * p.GetUpgradeLevel(u.id);
                    switch (u.stat)
                    {
                        case StatType.Attack: atkBonus += v; break;
                        case StatType.MaxHp: hpBonus += v; break;
                        case StatType.AttackSpeed: speedBonus += v; break;
                        case StatType.SkillDamage: skillBonus += v; break;
                        case StatType.OfflineEfficiency: offlineBonus += v; break;
                        case StatType.CritChance: critBonus += v; break;
                    }
                }
            }

            // Equipped items add to the same bonus pools as upgrades.
            for (int s = 0; s < PlayerProgress.EquipSlotCount; s++)
            {
                var item = eco.FindItem(p.GetEquippedId((EquipSlot)s));
                if (item == null || item.modifiers == null) continue;
                foreach (var m in item.modifiers)
                {
                    switch (m.stat)
                    {
                        case StatType.Attack: atkBonus += m.value; break;
                        case StatType.MaxHp: hpBonus += m.value; break;
                        case StatType.AttackSpeed: speedBonus += m.value; break;
                        case StatType.SkillDamage: skillBonus += m.value; break;
                        case StatType.OfflineEfficiency: offlineBonus += m.value; break;
                        case StatType.CritChance: critBonus += m.value; break;
                    }
                }
            }

            float realmAtk = 1f, realmHp = 1f;
            if (eco.realms != null)
                for (int i = 0; i <= p.RealmIndex && i < eco.realms.Length; i++)
                {
                    realmAtk *= eco.realms[i].attackMultiplier;
                    realmHp *= eco.realms[i].hpMultiplier;
                }

            return new PlayerStats
            {
                Attack = cfg.playerAttack * (1f + atkBonus) * realmAtk,
                MaxHp = cfg.playerMaxHp * (1f + hpBonus) * realmHp,
                AttackInterval = Math.Max(eco.minAttackInterval, cfg.playerAttackInterval / (1f + speedBonus)),
                CritChance = Clamp(eco.baseCritChance + critBonus, 0f, eco.maxCritChance),
                CritMultiplier = eco.baseCritMultiplier,
                SkillDamageMultiplier = 1f + skillBonus,
                DamageReduction = Clamp(eco.baseDamageReduction, 0f, eco.maxDamageReduction),
                OfflineEfficiency = Clamp(eco.baseOfflineEfficiency + offlineBonus, 0f, eco.maxOfflineEfficiency)
            };
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
