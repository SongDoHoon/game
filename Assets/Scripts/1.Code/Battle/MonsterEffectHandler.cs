public static class MonsterEffectHandler
{
    public static void ApplyBasicAttackDebuff(UnitController attacker, MonsterController target)
    {
        if (attacker == null || target == null) return;

        PassiveSkillData passive = attacker.Data.passiveSkillData;
        if (passive == null) return;

        if (passive.passiveDebuffType == DebuffType.Burn)
        {
            target.AddDebuff(new DebuffInstance
            {
                debuffType = DebuffType.Burn,
                value = passive.value1,
                duration = passive.value2,
                remainTime = passive.value2,
                stack = 1,
                maxStack = passive.maxStack <= 0 ? 1 : passive.maxStack,
                source = attacker
            });
        }
    }

    public static void ApplyCrowdControl(UnitController attacker, MonsterController target)
    {
        if (attacker == null || target == null) return;

        float duration = target.monsterType == MonsterType.Boss ? 1f : 2.5f;

        target.AddDebuff(new DebuffInstance
        {
            debuffType = DebuffType.Stun,
            value = 0f,
            duration = duration,
            remainTime = duration,
            source = attacker
        });
    }

    public static void ApplySkillDebuff(UnitController attacker, MonsterController target, ActiveSkillData active)
    {
        if (attacker == null || target == null || active == null) return;

        target.AddDebuff(new DebuffInstance
        {
            debuffType = DebuffType.Slow,
            value = active.value1,
            duration = active.duration,
            remainTime = active.duration,
            stack = 1,
            maxStack = active.maxDebuffStack <= 0 ? 1 : active.maxDebuffStack,
            source = attacker
        });
    }
}