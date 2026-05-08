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
    Right
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
}
