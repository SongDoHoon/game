using UnityEngine;

public static class UnitAttackHandler
{
    public static void ExecuteBasicAttack(UnitController attacker, MonsterController primaryTarget)
    {
        if (attacker == null || primaryTarget == null) return;

        UnitData data = attacker.Data;

        switch (data.basicAttackType)
        {
            case BasicAttackType.SingleMelee:
            case BasicAttackType.SingleRanged:
                SpawnProjectile(attacker, primaryTarget, false);
                break;

            case BasicAttackType.AoEMelee:
            case BasicAttackType.AoERanged:
                SpawnProjectile(attacker, primaryTarget, true);
                break;

        }
    }

    private static void SpawnProjectile(UnitController attacker, MonsterController target, bool isAreaAttack)
    {
        if (attacker == null || target == null || attacker.Data == null)
            return;

        BasicAttackProjectile.Spawn(
            attacker,
            target,
            attacker.CurrentAttackPower,
            isAreaAttack,
            attacker.Data.attackRadius);
    }
}
