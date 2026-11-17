namespace PixelImmortal.Data
{
    public enum ResourceType
    {
        LinhThach = 0,  // main currency, upgrades
        LinhKhi = 1,    // cultivation / breakthrough
        TinhHoa = 2     // skill upgrades
    }

    public enum StatType
    {
        Attack,
        MaxHp,
        AttackSpeed,
        SkillDamage,
        OfflineEfficiency,
        CritChance
    }

    public enum SkillKind
    {
        SingleTarget,
        Projectile,
        AreaAtTarget,
        SelfShield,
        AreaAroundPlayer
    }
}
