public static class DamageSystem
{
    public static void DealDamage(UnitController attacker, MonsterController target, float baseDamage, DamageType damageType)
    {
        if (attacker == null || target == null || !target.IsAlive) return;

        float finalDamage = baseDamage;

        ApplyExecute(attacker, target, ref finalDamage);
        ApplySpecialDamageLogic(attacker, target, ref finalDamage);

        string attackerName = attacker.Data != null ? attacker.Data.unitName : attacker.gameObject.name;
        string targetName = target.gameObject.name;

        UnityEngine.Debug.Log($"[{attackerName}] -> [{targetName}] µ¥¹ÌÁö: {finalDamage:F1}");

        target.TakeDamage(finalDamage);
    }

    private static void ApplyExecute(UnitController attacker, MonsterController target, ref float finalDamage)
    {
        PassiveSkillData passive = attacker.Data.passiveSkillData;
        if (passive == null || !passive.hasExecute) return;

        if (target.monsterType == MonsterType.Boss)
            return;

        if (target.GetHpPercent() <= passive.executeHpPercent)
            finalDamage = 9999999f;
    }

    private static void ApplySpecialDamageLogic(UnitController attacker, MonsterController target, ref float finalDamage)
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
        }
    }
}