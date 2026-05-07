public static class DamageSystem
{
    public static bool DealDamage(UnitController attacker, MonsterController target, double baseDamage)
    {
        if (attacker == null || target == null || !target.IsAlive)
            return false;

        bool wasAlive = target.IsAlive;

        if (UnitSkillHandler.ShouldExecuteLowHpTarget(attacker, target))
        {
            target.TakeDamage(target.CurrentHp);
            bool executed = wasAlive && !target.IsAlive;
            if (executed)
                UnitSkillHandler.OnMonsterKilled(attacker, target);

            return executed;
        }

        double finalDamage = baseDamage
            * target.GetDamageTakenMultiplier()
            * UnitSkillHandler.GetPassiveDamageMultiplier(attacker);

        if (UnityEngine.Random.value <= attacker.CurrentCritChance)
            finalDamage *= attacker.CurrentCritDamageMultiplier;

        target.TakeDamage(finalDamage);

        if (target.IsAlive && UnitSkillHandler.ShouldExecuteLowHpTarget(attacker, target))
            target.TakeDamage(target.CurrentHp);

        bool killed = wasAlive && !target.IsAlive;
        if (killed)
            UnitSkillHandler.OnMonsterKilled(attacker, target);

        return killed;
    }
}
