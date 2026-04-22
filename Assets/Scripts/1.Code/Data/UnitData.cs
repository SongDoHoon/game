using UnityEngine;

[CreateAssetMenu(menuName = "TD/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitId;
    public string unitName;
    public UnitGrade grade;
    public SpecialUnitLogicType specialLogicType = SpecialUnitLogicType.None;

    [Header("Combat")]
    public float attackPower = 10f;
    public float attackSpeed = 1f;
    public float attackRange = 3f;
    public float attackRadius = 1.5f;
    public int multiTargetCount = 2;
    public int pierceCount = 3;

    public BasicAttackType basicAttackType;
    public DamageType damageType;
    public TargetType targetType;

    [Header("Crowd Control Basic Attack")]
    [Range(0f, 1f)] public float crowdControlSlowAmount = 0.15f;
    public float crowdControlDuration = 2f;
    [Range(0f, 1f)] public float crowdControlBossSlowMultiplier = 0.5f;
    public float crowdControlBossDurationMultiplier = 0.6f;

    [Header("Skill")]
    public SkillTriggerType skillTriggerType = SkillTriggerType.None;
    public PassiveSkillData passiveSkillData;
    public ActiveSkillData activeSkillData;

    [Header("Evolution")]
    public bool canEvolve;
    public string evolveKey;
}
