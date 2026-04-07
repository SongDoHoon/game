using System.Collections;
using UnityEngine;

public static class UnitSkillHandler
{
    public static void ApplyPassiveOnStart(UnitController unit)
    {
        PassiveSkillData passive = unit.Data.passiveSkillData;
        if (passive == null) return;

        if (!passive.useStack && passive.passiveBuffType == BuffType.AttackPowerUp && passive.value1 > 0f)
        {
            unit.AddBuff(new BuffInstance
            {
                buffType = BuffType.AttackPowerUp,
                value = passive.value1,
                duration = 99999f,
                remainTime = 99999f,
                source = unit,
                isRuntime = false
            });
        }
    }

    public static void ApplyPassiveStatModifier(UnitController unit)
    {
        PassiveSkillData passive = unit.Data.passiveSkillData;
        if (passive == null) return;

        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Gabriel:
                unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, 0.2f, 0.1f);
                break;

            case SpecialUnitLogicType.Uriel:
                if (passive.useStack)
                {
                    float bonus = passive.value1 * unit.CurrentPassiveStack;
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, bonus, 0.1f);
                    unit.IsUrielMaxStackReached = unit.CurrentPassiveStack >= passive.maxStack;
                }
                break;

            case SpecialUnitLogicType.Raguel:
                if (passive.useStack)
                {
                    float bonus = passive.value1 * unit.CurrentPassiveStack;
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, bonus, 0.1f);
                }
                break;
        }
    }

    public static void OnBasicAttack(UnitController unit, MonsterController target)
    {
        if (unit == null || target == null) return;

        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Uriel:
                unit.AddPassiveStack(1);

                if (unit.IsUrielMaxStackReached)
                {
                    target.AddDebuff(new DebuffInstance
                    {
                        debuffType = DebuffType.Burn,
                        value = Mathf.Max(1f, unit.CurrentAttackPower * 0.2f),
                        duration = 3f,
                        remainTime = 3f,
                        stack = 1,
                        maxStack = 20,
                        source = unit
                    });
                }
                break;
        }
    }

    public static void ExecuteActiveSkill(UnitController unit)
    {
        ActiveSkillData active = unit.Data.activeSkillData;
        if (active == null) return;

        switch (active.castType)
        {
            case ActiveSkillCastType.SelfBuff:
                ExecuteSelfBuff(unit, active);
                break;

            case ActiveSkillCastType.AllyBuff:
                ExecuteAllyBuff(unit, active);
                break;

            case ActiveSkillCastType.EnemyAreaDamage:
                unit.StartCoroutine(CoAreaDamage(unit, active));
                break;

            case ActiveSkillCastType.EnemyAreaDebuff:
                ExecuteAreaDebuff(unit, active);
                break;

            case ActiveSkillCastType.MapTargetStrike:
                unit.StartCoroutine(CoAreaDamage(unit, active));
                break;

            case ActiveSkillCastType.SummonZoneEffect:
                ExecuteAreaDebuff(unit, active);
                break;
        }

        ApplySpecialActive(unit, active);
    }

    private static void ExecuteSelfBuff(UnitController unit, ActiveSkillData active)
    {
        unit.AddBuff(new BuffInstance
        {
            buffType = BuffType.AttackPowerUp,
            value = active.value1,
            duration = active.duration,
            remainTime = active.duration,
            source = unit,
            isRuntime = false
        });
    }

    private static void ExecuteAllyBuff(UnitController unit, ActiveSkillData active)
    {
        UnitController[] allies = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        foreach (UnitController ally in allies)
        {
            ally.AddBuff(new BuffInstance
            {
                buffType = BuffType.AttackPowerUp,
                value = active.value1,
                duration = active.duration,
                remainTime = active.duration,
                source = unit,
                isRuntime = false
            });
        }

        if (unit.Data.specialLogicType == SpecialUnitLogicType.Raguel)
        {
            unit.AddPassiveStack(1);
        }
    }

    private static IEnumerator CoAreaDamage(UnitController unit, ActiveSkillData active)
    {
        int count = Mathf.Max(1, active.hitCount);
        float interval = Mathf.Max(0.05f, active.interval);

        for (int i = 0; i < count; i++)
        {
            MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

            foreach (MonsterController monster in monsters)
            {
                if (!monster.IsAlive) continue;

                if (Vector3.Distance(unit.transform.position, monster.transform.position) <= active.radius)
                {
                    DamageSystem.DealDamage(unit, monster, unit.CurrentAttackPower * Mathf.Max(1f, active.value1), unit.Data.damageType);
                }
            }

            yield return new WaitForSeconds(interval);
        }
    }

    private static void ExecuteAreaDebuff(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            if (Vector3.Distance(unit.transform.position, monster.transform.position) <= active.radius)
            {
                MonsterEffectHandler.ApplySkillDebuff(unit, monster, active);
            }
        }
    }

    private static void ApplySpecialActive(UnitController unit, ActiveSkillData active)
    {
        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Michael:
                ExecuteAllyBuff(unit, active);
                break;

            case SpecialUnitLogicType.Gabriel:
                unit.StartCoroutine(CoAreaDamage(unit, active));
                break;

            case SpecialUnitLogicType.Raphael:
                unit.StartCoroutine(CoAreaDamage(unit, active));
                break;

            case SpecialUnitLogicType.Uriel:
                unit.StartCoroutine(CoUrielFirePillar(unit, active));
                break;

            case SpecialUnitLogicType.Sariel:
                ExecuteSarielControl(unit, active);
                break;
        }
    }

    private static IEnumerator CoUrielFirePillar(UnitController unit, ActiveSkillData active)
    {
        int count = active.hitCount > 0 ? active.hitCount : 6;
        float interval = active.interval > 0f ? active.interval : 0.4f;

        for (int i = 0; i < count; i++)
        {
            MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

            foreach (MonsterController monster in monsters)
            {
                if (!monster.IsAlive) continue;

                if (Vector3.Distance(unit.transform.position, monster.transform.position) <= active.radius)
                {
                    DamageSystem.DealDamage(unit, monster, unit.CurrentAttackPower * Mathf.Max(1f, active.value1), DamageType.Fire);

                    monster.AddDebuff(new DebuffInstance
                    {
                        debuffType = DebuffType.Burn,
                        value = Mathf.Max(1f, unit.CurrentAttackPower * 0.25f),
                        duration = active.duration,
                        remainTime = active.duration,
                        stack = 1,
                        maxStack = active.maxDebuffStack > 0 ? active.maxDebuffStack : 20,
                        source = unit
                    });
                }
            }

            yield return new WaitForSeconds(interval);
        }
    }

    private static void ExecuteSarielControl(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            if (Vector3.Distance(unit.transform.position, monster.transform.position) <= active.radius)
            {
                float duration = monster.monsterType == MonsterType.Boss ? 1f : 2.5f;

                monster.AddDebuff(new DebuffInstance
                {
                    debuffType = DebuffType.Stun,
                    duration = duration,
                    remainTime = duration,
                    source = unit
                });
            }
        }
    }
}