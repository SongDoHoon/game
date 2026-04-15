using UnityEngine;

public static class MonsterEffectHandler
{
    public static float GetEffectAdjustedValue(MonsterController target, SkillEffectData effect)
    {
        if (target == null || effect == null)
            return 0f;

        if (!effect.affectsBossDifferently || target.monsterType != MonsterType.Boss)
            return effect.value;

        return effect.value * effect.bossMultiplier;
    }

    public static float GetEffectAdjustedDuration(MonsterController target, SkillEffectData effect)
    {
        if (effect == null)
            return 0f;

        if (target == null || !effect.affectsBossDifferently || target.monsterType != MonsterType.Boss)
            return effect.duration;

        return effect.duration * effect.bossMultiplier;
    }

    public static void ApplyDebuff(UnitController attacker, MonsterController target, DebuffType debuffType, float value, float duration, int maxStack = 1)
    {
        if (attacker == null || target == null) return;

        target.AddDebuff(new DebuffInstance
        {
            debuffType = debuffType,
            value = value,
            duration = duration,
            remainTime = duration,
            stack = 1,
            maxStack = Mathf.Max(1, maxStack),
            source = attacker
        });
    }

    public static void ApplyBasicAttackDebuff(UnitController attacker, MonsterController target)
    {
        if (attacker == null || target == null) return;

        PassiveSkillData passive = attacker.Data.passiveSkillData;
        if (passive == null) return;

        if (passive.passiveDebuffType == DebuffType.Burn)
        {
            ApplyDebuff(attacker, target, DebuffType.Burn, passive.value1, passive.value2, passive.maxStack <= 0 ? 1 : passive.maxStack);
        }
    }

    public static void ApplyCrowdControl(UnitController attacker, MonsterController target)
    {
        if (attacker == null || target == null) return;

        float duration = target.monsterType == MonsterType.Boss ? 1f : 2.5f;

        ApplyDebuff(attacker, target, DebuffType.Stun, 0f, duration);
    }

    public static void ApplySkillDebuff(UnitController attacker, MonsterController target, ActiveSkillData active)
    {
        if (attacker == null || target == null || active == null) return;

        ApplyDebuff(attacker, target, DebuffType.Slow, active.value1, active.duration, active.maxDebuffStack <= 0 ? 1 : active.maxDebuffStack);
    }

    public static void ApplyEffectDebuff(UnitController attacker, MonsterController target, SkillEffectData effect)
    {
        if (attacker == null || target == null || effect == null || effect.effectType != SkillEffectType.Debuff)
            return;

        float value = GetEffectAdjustedValue(target, effect);
        float duration = GetEffectAdjustedDuration(target, effect);
        int maxStack = effect.maxStack > 0 ? effect.maxStack : 1;

        ApplyDebuff(attacker, target, effect.debuffType, value, duration, maxStack);
    }
}
