public enum UnitGrade
{
    Normal = 0,
    Rare = 5,
    Epic = 1,
    Verure = 2,
    ArchAngel = 3,
    GreatDemon = 4
}

public enum BasicAttackType
{
    SingleMelee,
    SingleRanged,
    AoEMelee,
    AoERanged
}

public enum UnitTargetPriority
{
    Nearest,
    Farthest,
    Boss,
    LowestHp,
    Elite,
    BossOrElite
}

public enum BuffType
{
    AttackPowerUp,
    AttackSpeedUp,
    RangeUp,
    CritChanceUp,
    FinalDamageUp,
    BurnDamageUp,
    AllStatUp,
    CritDamageUp
}

public enum DebuffType
{
    None,
    Burn,
    Slow,
    Stun,
    DefenseDown,
    DamageTakenUp,
    Silence,
    CorruptionLord
}

public enum MonsterType
{
    Normal,
    Elite,
    BountyElite,
    Boss
}

public enum EvolutionItemType
{
    None,
    Baekho,
    Cheongryong,
    Hyeonmu,
    Jujak,
    Taotie,
    Qiongqi,
    Taowu,
    Hundun
}

public enum UnitEnhanceGroup
{
    LowGradeGroup,
    HighGradeGroup,
    EvolutionGroup
}

public enum ManualEnhanceResult
{
    Success,
    Failed,
    NotEnoughGold,
    MaxStack,
    NotAvailable
}
