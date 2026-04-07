using System.Collections.Generic;
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
                DealSingle(attacker, primaryTarget);
                break;

            case BasicAttackType.AoEMelee:
            case BasicAttackType.AoERanged:
                AttackArea(attacker, primaryTarget.transform.position, data.attackRadius);
                break;

            case BasicAttackType.MultiTargetRanged:
                AttackMulti(attacker, data.multiTargetCount);
                break;

            case BasicAttackType.PierceRanged:
                AttackPierce(attacker, data.pierceCount);
                break;

            case BasicAttackType.DebuffRanged:
                DealSingle(attacker, primaryTarget);
                MonsterEffectHandler.ApplyBasicAttackDebuff(attacker, primaryTarget);
                break;

            case BasicAttackType.CrowdControlRanged:
                DealSingle(attacker, primaryTarget);
                MonsterEffectHandler.ApplyCrowdControl(attacker, primaryTarget);
                break;
        }

        UnitSkillHandler.OnBasicAttack(attacker, primaryTarget);
    }

    private static void DealSingle(UnitController attacker, MonsterController target)
    {
        DamageSystem.DealDamage(attacker, target, attacker.CurrentAttackPower, attacker.Data.damageType);
    }

    private static void AttackArea(UnitController attacker, Vector3 center, float radius)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            if (Vector3.Distance(center, monster.transform.position) <= radius)
            {
                DamageSystem.DealDamage(attacker, monster, attacker.CurrentAttackPower, attacker.Data.damageType);
            }
        }
    }

    private static void AttackMulti(UnitController attacker, int count)
    {
        List<MonsterController> targets = attacker.GetTargetsInRangeSorted();
        int hit = 0;

        foreach (MonsterController monster in targets)
        {
            DamageSystem.DealDamage(attacker, monster, attacker.CurrentAttackPower, attacker.Data.damageType);
            hit++;

            if (hit >= count) break;
        }
    }

    private static void AttackPierce(UnitController attacker, int pierceCount)
    {
        List<MonsterController> targets = attacker.GetTargetsInRangeSorted();
        int hit = 0;

        foreach (MonsterController monster in targets)
        {
            DamageSystem.DealDamage(attacker, monster, attacker.CurrentAttackPower, attacker.Data.damageType);
            hit++;

            if (hit >= pierceCount) break;
        }
    }
}