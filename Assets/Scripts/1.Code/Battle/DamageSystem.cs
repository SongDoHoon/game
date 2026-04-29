public static class DamageSystem
{
    public static void DealDamage(UnitController attacker, MonsterController target, double baseDamage, DamageType damageType)
    {
        if (attacker == null || target == null || !target.IsAlive) return;

        double finalDamage = baseDamage;

        ApplyExecute(attacker, target, ref finalDamage);
        ApplySpecialDamageLogic(attacker, target, ref finalDamage);
        finalDamage *= target.GetDamageTakenMultiplier();

        target.TakeDamage(finalDamage);

        if (!target.IsAlive)
            UnitSkillHandler.OnMonsterKilled(attacker, target);
    }

    private static void ApplyExecute(UnitController attacker, MonsterController target, ref double finalDamage)
    {
        PassiveSkillData passive = attacker.Data.passiveSkillData;
        if (passive == null || !passive.hasExecute) return;

        if (target.monsterType == MonsterType.Boss)
            return;

        if (target.GetHpPercent() <= passive.executeHpPercent)
            finalDamage = 9999999f;
    }

    private static void ApplySpecialDamageLogic(UnitController attacker, MonsterController target, ref double finalDamage)
    {
        switch (attacker.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Raphael:
                int targetCount = attacker.GetTargetsInRangeCount();
                finalDamage *= 1f + (0.1f * targetCount);
                break;

            case SpecialUnitLogicType.Uriel:
                if (attacker.IsUrielMaxStackReached)
                    finalDamage *= 1.2f;
                break;

            case SpecialUnitLogicType.Demon5:
                if (target.HasDebuff(DebuffType.Stun))
                    finalDamage *= 1.15f;
                break;
        }
    }
}
