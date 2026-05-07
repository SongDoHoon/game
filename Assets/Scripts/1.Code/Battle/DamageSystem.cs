public static class DamageSystem
{
    public static void DealDamage(UnitController attacker, MonsterController target, double baseDamage)
    {
        if (attacker == null || target == null || !target.IsAlive) return;

        double finalDamage = baseDamage * target.GetDamageTakenMultiplier();
        target.TakeDamage(finalDamage);
    }
}
