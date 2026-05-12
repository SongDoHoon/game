using UnityEngine;

public enum SkillTriggerType
{
    Cooldown,
    BasicAttackChance,
    BasicAttackCount
}

public enum SkillEffectType
{
    Damage,
    ApplyBuff,
    HorizontalLineDamage,
    RepeatedAreaDamage,
    ConeDamage
}

public enum SkillLineDirection
{
    Both,
    Left,
    Right,
    TowardTarget
}

public enum SkillLineOrigin
{
    CasterPosition,
    TargetPosition
}

[CreateAssetMenu(menuName = "TD/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillId;
    public string skillName;
    public bool isEnabled = true;

    [Header("Trigger")]
    public SkillTriggerType triggerType = SkillTriggerType.Cooldown;
    public SkillEffectType effectType = SkillEffectType.Damage;
    public float cooldown = 8f;
    public float attackLockDurationOnCast = 0.2f;
    public bool startWithCooldown;
    [Range(0f, 1f)] public float triggerChance = 1f;
    public int requiredBasicAttackCount = 3;

    [Header("Passive Aura")]
    public bool hasPassiveAura;
    public bool stackPassiveAuraWithSameSkill;
    public float passiveAllUnitAttackPowerBonus;
    public float passiveAllUnitAttackSpeedBonus;
    public float passiveAllMonsterMoveSpeedReduction;
    public int passiveAllMonsterMoveSpeedReductionMaxSameSkillCount = 1;
    public float passiveSelfAttackPowerBonus;
    public float passiveSelfAttackSpeedBonus;
    public float passiveDamageBonusPerEnemyInRange;
    public int passiveDamageBonusMaxEnemyCount;
    [Range(0f, 1f)] public float passiveExecuteHpPercent;
    public float passiveDebuffRadius = 0f;
    public DebuffType passiveDebuffType = DebuffType.None;
    public float passiveDebuffValue = 0f;
    public float passiveDebuffDuration = 0.2f;

    [Header("Targeting")]
    public bool useCurrentTargetFirst = true;
    public UnitTargetPriority targetPriority = UnitTargetPriority.Nearest;
    public UnitTargetPriority bossFallbackTargetPriority = UnitTargetPriority.Nearest;
    public float skillRange = 0f;

    [Header("Damage")]
    public float baseSkillDamageMultiplier = 2f;
    public float additionalSkillDamage = 0f;
    public float areaRadius = 0f;
    public bool activeHitsAllTargetsInRange;
    public bool activeTargetsEntireField;

    [Header("Horizontal Line Damage")]
    public SkillLineOrigin lineOrigin = SkillLineOrigin.CasterPosition;
    public SkillLineDirection lineDirection = SkillLineDirection.Both;
    public float lineLength = 6f;
    public float lineWidth = 1f;
    public bool showLineIndicator = true;
    public Color lineIndicatorColor = new Color(1f, 0.92f, 0.35f, 0.25f);
    public float lineIndicatorDuration = 0.25f;

    [Header("Passive Stack")]
    public int passiveStackGainOnBasicAttack = 0;
    public int passiveStackGainOnKill = 0;
    public int passiveBossOrEliteStackGainOnKill = 0;
    public int passiveStackGainOnAnyMonsterDeath = 0;
    public int passiveBossOrEliteStackGainOnAnyMonsterDeath = 0;
    public int maxPassiveStack = 0;
    public float attackPowerBonusPerPassiveStack = 0f;
    public float attackSpeedBonusPerPassiveStack = 0f;
    public int passiveStackGainOnBuffSkill = 0;
    public SkillBuffEffect[] passiveMaxStackBuffEffects = new SkillBuffEffect[0];
    public bool passiveStackTracksEnemiesInAttackRange;
    public bool passiveStackTracksSlowOrStunnedEnemies;
    public float cooldownReductionPerPassiveStack = 0f;
    public float passiveMaxStackAttackSpeedBonus = 0f;

    [Header("Manual Self Enhancement")]
    public bool hasManualSelfEnhancement;
    public int manualEnhanceMaxStack = 20;
    public int manualEnhanceBaseGoldCost = 100;
    public int manualEnhanceGoldCostIncrease = 10;
    [Range(0f, 1f)] public float manualEnhanceBaseSuccessChance = 0.05f;
    [Range(0f, 1f)] public float manualEnhanceSuccessChanceMultiplierPerSuccess = 0.5f;
    public float manualEnhanceAttackPowerBonusPerStack = 0.1f;
    public float manualEnhanceAttackSpeedBonusPerStack = 0.05f;

    [Header("Active Self Buff")]
    public float activeBuffDuration = 0f;
    public float activeSelfAttackPowerBonus = 0f;
    public float activeSelfRangeOverride = 0f;
    public float activeAttackPowerBonusPerEnemyInRange = 0f;
    public float activeAttackPowerBonusMax = 0f;

    [Header("Corruption Lord")]
    public bool hasCorruptionLord;
    public float corruptionLordDuration = 6f;
    public float corruptionLordTickInterval = 0.5f;
    public float corruptionLordMaxHpDamagePercentPerTick = 0.005f;
    public float corruptionLordActiveBonusMaxHpDamagePercent = 0.1f;

    [Header("Deep Sea Area")]
    public bool createDeepSeaAreaOnBasicAttackImpact;
    public float deepSeaAreaRadius = 1.5f;
    public float deepSeaAreaDuration = 5f;
    public int maxDeepSeaAreaCount = 2;
    public float deepSeaAreaSlow = 0.1f;
    public float deepSeaAreaDamageMultiplierPerSecond = 0.6f;
    public Color deepSeaAreaColor = new Color(0.05f, 0.45f, 0.8f, 0.35f);

    [Header("Deep Sea Explosion")]
    public bool activeExplodesDeepSeaAreas;
    public float deepSeaExplosionDuration = 5f;
    public float deepSeaExplosionDamageMultiplierPerSecond = 1.1f;
    public float deepSeaExplosionSlow = 0.35f;
    public Color deepSeaExplosionColor = new Color(0.25f, 0.9f, 1f, 0.45f);

    [Header("Repeated Area Damage")]
    public SkillLineOrigin repeatedDamageOrigin = SkillLineOrigin.TargetPosition;
    public int repeatedHitCount = 1;
    public float repeatedHitInterval = 0.5f;

    [Header("Cone Damage")]
    public float coneRange = 4f;
    [Range(1f, 360f)] public float coneAngle = 90f;
    public SkillLineDirection coneFallbackDirection = SkillLineDirection.Right;
    public bool aimConeAtTarget = true;

    [Header("Buff Effect")]
    public SkillBuffEffect[] buffEffects = new SkillBuffEffect[0];

    [Header("Debuff Effect")]
    public SkillDebuffEffect[] debuffEffects = new SkillDebuffEffect[0];

    [Header("Projectile")]
    public bool useProjectile = true;
    public float projectileSpeed = 10f;
    public float projectileSize = 0.35f;
    public Color projectileColor = new Color(1f, 0.92f, 0.35f, 1f);
    public Vector3 projectileSpawnOffset = new Vector3(0f, 0.45f, 0f);

    [Header("Area Indicator")]
    public bool showAreaAttackIndicator = true;
    public Color areaAttackIndicatorColor = new Color(1f, 0.92f, 0.35f, 0.25f);
    public float areaAttackIndicatorDuration = 0.25f;
}

[System.Serializable]
public class SkillBuffEffect
{
    public BuffType buffType;
    public float value;
    public float duration;
    public bool applyToAllUnits = true;
}

[System.Serializable]
public class SkillDebuffEffect
{
    public DebuffType debuffType = DebuffType.None;
    public float value;
    public float duration;
    public int stack = 1;
    public int maxStack = 1;
    public float damageMultiplierOnExpire = 0f;
    public float currentHpDamagePercentOnExpire = 0f;
}

[System.Serializable]
public class BuffInstance
{
    public BuffType buffType;
    public float value;
    public float duration;
    public float remainTime;
    public UnitController source;
    public bool isRuntime;
}

[System.Serializable]
public class DebuffInstance
{
    public DebuffType debuffType;
    public float value;
    public float duration;
    public float remainTime;
    public int stack = 1;
    public int maxStack = 1;
    public UnitController source;
    public float damageMultiplierOnExpire;
    public float currentHpDamagePercentOnExpire;
    public float maxHpDamagePercentPerTick;
    public float tickInterval;
    public float tickTimer;
}
