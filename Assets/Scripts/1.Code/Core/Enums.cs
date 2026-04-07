public enum UnitGrade
{
    Normal,
    Epic,
    Verure,
    ArchAngel,
    GreatDemon
}

public enum BasicAttackType
{
    SingleMelee,
    SingleRanged,
    AoEMelee,
    AoERanged,
    MultiTargetRanged,
    PierceRanged,
    DebuffRanged,
    CrowdControlRanged
}

public enum DamageType
{
    Physical,
    Magic,
    Fire,
    Holy,
    Dark
}

public enum SkillTriggerType
{
    None,
    Passive,
    Active,
    PassiveAndActive
}

public enum ActiveSkillCastType
{
    None,
    SelfBuff,
    AllyBuff,
    EnemyAreaDamage,
    EnemyAreaDebuff,
    SummonZoneEffect,
    MapTargetStrike
}

public enum TargetType
{
    None,
    SingleEnemy,
    MultiEnemy,
    AreaEnemy,
    SingleAlly,
    AreaAlly,
    AllAllies,
    Self
}

public enum BuffType
{
    AttackPowerUp,
    AttackSpeedUp,
    RangeUp,
    CritChanceUp,
    FinalDamageUp,
    BurnDamageUp,
    AllStatUp
}

public enum DebuffType
{
    None,
    Burn,
    Slow,
    Stun,
    DefenseDown,
    DamageTakenUp,
    Silence
}

public enum MonsterType
{
    Normal,
    Elite,
    Boss
}

public enum EvolutionItemType
{
    None,
    MichaelItem,
    GabrielItem,
    RaphaelItem,
    UrielItem,
    RaguelItem,
    SarielItem,
    DemonItem1,
    DemonItem2,
    DemonItem3,
    DemonItem4,
    DemonItem5,
    DemonItem6,
    DemonItem7
}

public enum SpecialUnitLogicType
{
    None,
    Michael,
    Gabriel,
    Raphael,
    Uriel,
    Raguel,
    Sariel,
    Demon1,
    Demon2,
    Demon3,
    Demon4,
    Demon5,
    Demon6,
    Demon7
}