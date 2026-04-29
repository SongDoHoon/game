using System.Collections;
using UnityEngine;

public static class UnitSkillHandler
{
    public static void UpdateContinuousEffects(UnitController unit)
    {
        if (unit == null || unit.Data == null) return;

        PassiveSkillData passive = unit.Data.passiveSkillData;
        if (passive == null) return;

        ApplyPassiveEffects(unit, passive, SkillEffectTrigger.OnContinuous);

        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Demon7:
                ApplyBelphegorAura(unit, passive);
                break;
        }
    }

    public static void ApplyPassiveOnStart(UnitController unit)
    {
        PassiveSkillData passive = unit.Data.passiveSkillData;
        if (passive == null) return;

        ApplyPassiveEffects(unit, passive, SkillEffectTrigger.OnInitialize);

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

        ApplyPassiveEffects(unit, passive, SkillEffectTrigger.OnStatRecalculation);

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

            case SpecialUnitLogicType.Demon1:
                if (passive.useStack)
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, passive.value1 * unit.CurrentPassiveStack, 0.1f);
                break;

            case SpecialUnitLogicType.Demon2:
                if (passive.useStack)
                {
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, passive.value1 * unit.CurrentPassiveStack, 0.1f);
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackSpeedUp, passive.value2 * unit.CurrentPassiveStack, 0.1f);
                }
                break;

            case SpecialUnitLogicType.Demon4:
                int nearbyEnemyCount = unit.GetNearbyEnemyCount(unit.CurrentAttackRange);
                int satanCountCap = passive.maxStack > 0 ? passive.maxStack : nearbyEnemyCount;
                int satanAppliedCount = Mathf.Min(nearbyEnemyCount, satanCountCap);

                if (satanAppliedCount > 0)
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, passive.value1 * satanAppliedCount, 0.1f);

                if (satanCountCap > 0 && satanAppliedCount >= satanCountCap && passive.value2 > 0f)
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackSpeedUp, passive.value2, 0.1f);
                break;

            case SpecialUnitLogicType.Demon5:
                int crowdControlledEnemyCount = unit.CountEnemiesWithDebuffs(DebuffType.Slow, DebuffType.Stun);
                int asmodeusCountCap = passive.maxStack > 0 ? passive.maxStack : crowdControlledEnemyCount;
                int asmodeusAppliedCount = Mathf.Min(crowdControlledEnemyCount, asmodeusCountCap);

                if (asmodeusAppliedCount > 0)
                    unit.AddOrRefreshRuntimeBuff(BuffType.AttackPowerUp, passive.value1 * asmodeusAppliedCount, 0.1f);
                break;
        }
    }

    public static void OnBasicAttack(UnitController unit, MonsterController target)
    {
        if (unit == null || target == null) return;

        ApplyPassiveEffects(unit, unit.Data.passiveSkillData, SkillEffectTrigger.OnBasicAttack, target);

        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Uriel:
                unit.AddPassiveStack(1);

                if (unit.IsUrielMaxStackReached)
                {
                    MonsterEffectHandler.ApplyDebuff(unit, target, DebuffType.Burn, Mathf.Max(1f, unit.CurrentAttackPower * 0.2f), 3f, 20);
                }
                break;

            case SpecialUnitLogicType.Demon2:
                PassiveSkillData mammonPassive = unit.Data.passiveSkillData;
                if (mammonPassive != null && Random.value <= Mathf.Clamp01(mammonPassive.value3))
                    unit.AddPassiveStack(1);
                break;

            case SpecialUnitLogicType.Demon3:
                PassiveSkillData leviathanPassive = unit.Data.passiveSkillData;
                if (leviathanPassive != null)
                {
                    MonsterEffectHandler.ApplyDebuff(unit, target, DebuffType.Slow, leviathanPassive.value1, leviathanPassive.value3);
                    MonsterEffectHandler.ApplyDebuff(unit, target, DebuffType.Burn, Mathf.Max(1f, unit.CurrentAttackPower * leviathanPassive.value2), leviathanPassive.value3);
                }
                break;

            case SpecialUnitLogicType.Demon6:
                PassiveSkillData beelzebubPassive = unit.Data.passiveSkillData;
                if (beelzebubPassive != null)
                {
                    float poisonDamagePerSecond = target.MaxHp * beelzebubPassive.value1;
                    MonsterEffectHandler.ApplyDebuff(unit, target, DebuffType.Burn, poisonDamagePerSecond, beelzebubPassive.value2);
                    MonsterEffectHandler.ApplyDebuff(unit, target, DebuffType.DamageTakenUp, beelzebubPassive.value3, beelzebubPassive.value2);
                }
                break;
        }
    }

    public static void OnMonsterKilled(UnitController unit, MonsterController target)
    {
        if (unit == null || unit.Data == null || target == null) return;

        ApplyPassiveEffects(unit, unit.Data.passiveSkillData, SkillEffectTrigger.OnKill, target);

        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Demon1:
                unit.AddPassiveStack(target.monsterType == MonsterType.Boss || target.monsterType == MonsterType.Elite ? 2 : 1);
                break;
        }
    }

    public static void ExecuteActiveSkill(UnitController unit)
    {
        if (unit == null || unit.Data == null)
            return;

        ActiveSkillData active = unit.Data.activeSkillData;
        if (active == null) return;

        if (HasEffects(active.effects))
        {
            ExecuteExtendedActiveSkill(unit, active);
            return;
        }

        if (TryExecuteSpecialActive(unit, active))
            return;

        ExecuteLegacyActiveSkill(unit, active);
    }

    private static void ExecuteLegacyActiveSkill(UnitController unit, ActiveSkillData active)
    {
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
                    DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value1), unit.Data.damageType);
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

    private static bool TryExecuteSpecialActive(UnitController unit, ActiveSkillData active)
    {
        switch (unit.Data.specialLogicType)
        {
            case SpecialUnitLogicType.Michael:
                ExecuteAllyBuff(unit, active);
                return true;

            case SpecialUnitLogicType.Gabriel:
                unit.StartCoroutine(CoAreaDamage(unit, active));
                return true;

            case SpecialUnitLogicType.Raphael:
                unit.StartCoroutine(CoAreaDamage(unit, active));
                return true;

            case SpecialUnitLogicType.Uriel:
                unit.StartCoroutine(CoUrielFirePillar(unit, active));
                return true;

            case SpecialUnitLogicType.Sariel:
                ExecuteSarielControl(unit, active);
                return true;

            case SpecialUnitLogicType.Demon1:
                ExecuteLuciferCleave(unit, active);
                return true;

            case SpecialUnitLogicType.Demon2:
                ExecuteMammonOverdrive(unit, active);
                return true;

            case SpecialUnitLogicType.Demon3:
                ExecuteLeviathanBurst(unit, active);
                return true;

            case SpecialUnitLogicType.Demon4:
                ExecuteSatanCollapse(unit, active);
                return true;

            case SpecialUnitLogicType.Demon5:
                unit.StartCoroutine(CoAsmodeusChain(unit, active));
                return true;

            case SpecialUnitLogicType.Demon6:
                ExecuteBeelzebubPlague(unit, active);
                return true;

            case SpecialUnitLogicType.Demon7:
                unit.StartCoroutine(CoBelphegorSloth(unit, active));
                return true;
        }

        return false;
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
                    DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value1), DamageType.Fire);

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

                MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Stun, 0f, duration);
            }
        }
    }

    private static void ExecuteLuciferCleave(UnitController unit, ActiveSkillData active)
    {
        ApplyDamageInRadius(unit, active.radius, active.value1, unit.Data.damageType);
    }

    private static void ExecuteMammonOverdrive(UnitController unit, ActiveSkillData active)
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

        unit.AddBuff(new BuffInstance
        {
            buffType = BuffType.AttackSpeedUp,
            value = active.value2,
            duration = active.duration,
            remainTime = active.duration,
            source = unit,
            isRuntime = false
        });
    }

    private static void ExecuteExtendedActiveSkill(UnitController unit, ActiveSkillData active)
    {
        ApplyActiveEffects(unit, active, SkillEffectTrigger.OnActiveCast);

        if (HasEffects(active.effects, SkillEffectTrigger.OnActiveTick) || HasEffects(active.effects, SkillEffectTrigger.OnActiveEnd))
        {
            unit.StartCoroutine(CoExtendedActiveSequence(unit, active));
        }
    }

    private static IEnumerator CoExtendedActiveSequence(UnitController unit, ActiveSkillData active)
    {
        SkillEffectData tickTemplate = GetFirstEffect(active.effects, SkillEffectTrigger.OnActiveTick);
        int tickCount = tickTemplate != null && tickTemplate.hitCount > 0 ? tickTemplate.hitCount : 1;
        float tickInterval = tickTemplate != null && tickTemplate.interval > 0f ? tickTemplate.interval : 0.5f;

        if (HasEffects(active.effects, SkillEffectTrigger.OnActiveTick))
        {
            for (int i = 0; i < tickCount; i++)
            {
                ApplyActiveEffects(unit, active, SkillEffectTrigger.OnActiveTick);

                if (i < tickCount - 1)
                    yield return new WaitForSeconds(tickInterval);
            }
        }

        SkillEffectData endTemplate = GetFirstEffect(active.effects, SkillEffectTrigger.OnActiveEnd);
        float endDelay = endTemplate != null && endTemplate.duration > 0f ? endTemplate.duration : 0f;
        if (endDelay > 0f)
            yield return new WaitForSeconds(endDelay);

        ApplyActiveEffects(unit, active, SkillEffectTrigger.OnActiveEnd);
    }

    private static void ExecuteLeviathanBurst(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            if (!monster.HasDebuff(DebuffType.Slow, unit)) continue;

            DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value1), unit.Data.damageType);
            MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Slow, active.value2, active.duration);
        }
    }

    private static void ExecuteSatanCollapse(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            if (Vector3.Distance(unit.transform.position, monster.transform.position) > active.radius) continue;

            DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value1), unit.Data.damageType);

            float stunDuration = monster.monsterType == MonsterType.Boss ? Mathf.Min(1f, active.duration) : active.duration;
            MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Stun, 0f, stunDuration);
        }
    }

    private static IEnumerator CoAsmodeusChain(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            if (Vector3.Distance(unit.transform.position, monster.transform.position) > active.radius) continue;

            DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value1), unit.Data.damageType);
            MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Stun, 0f, monster.monsterType == MonsterType.Boss ? Mathf.Min(1f, active.duration) : active.duration);
        }

        yield return new WaitForSeconds(active.duration);

        monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            if (Vector3.Distance(unit.transform.position, monster.transform.position) > active.radius) continue;

            DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value2), unit.Data.damageType);

            if (active.value3 > 0f)
            {
                float secondaryStunDuration = monster.monsterType == MonsterType.Boss ? Mathf.Min(1f, active.value3) : active.value3;
                MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Stun, 0f, secondaryStunDuration);
            }
        }
    }

    private static void ExecuteBeelzebubPlague(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            if (Vector3.Distance(unit.transform.position, monster.transform.position) > active.radius) continue;

            DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, active.value1), unit.Data.damageType);

            if (monster.HasDebuff(DebuffType.Burn, unit))
                monster.TakeDamage(monster.CurrentHp * Mathf.Max(0f, active.value2));
        }
    }

    private static IEnumerator CoBelphegorSloth(UnitController unit, ActiveSkillData active)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Slow, active.value1, active.duration);
        }

        yield return new WaitForSeconds(active.duration);

        monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            monster.TakeDamage(monster.CurrentHp * Mathf.Max(0f, active.value2));
        }
    }

    private static void ApplyBelphegorAura(UnitController unit, PassiveSkillData passive)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            MonsterEffectHandler.ApplyDebuff(unit, monster, DebuffType.Slow, passive.value1, 0.25f);
        }
    }

    private static void ApplyDamageInRadius(UnitController unit, float radius, float damageMultiplier, DamageType damageType)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;
            if (Vector3.Distance(unit.transform.position, monster.transform.position) > radius) continue;

            DamageSystem.DealDamage(unit, monster, GetSkillDamage(unit, damageMultiplier), damageType);
        }
    }

    private static void ApplyPassiveEffects(UnitController unit, PassiveSkillData passive, SkillEffectTrigger trigger, MonsterController target = null)
    {
        if (unit == null || passive == null || !HasEffects(passive.effects, trigger))
            return;

        bool useRuntimeBuff = trigger == SkillEffectTrigger.OnStatRecalculation || trigger == SkillEffectTrigger.OnContinuous;

        foreach (SkillEffectData effect in passive.effects)
        {
            if (effect == null || effect.trigger != trigger)
                continue;

            ApplyEffect(unit, effect, target, useRuntimeBuff);
        }
    }

    private static void ApplyActiveEffects(UnitController unit, ActiveSkillData active, SkillEffectTrigger trigger)
    {
        if (unit == null || active == null || !HasEffects(active.effects, trigger))
            return;

        UnitController[] allies = null;
        MonsterController[] monsters = null;

        if (active.targetType == TargetType.AllAllies || active.targetType == TargetType.AreaAlly || active.targetType == TargetType.SingleAlly)
            allies = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        else
            monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (SkillEffectData effect in active.effects)
        {
            if (effect == null || effect.trigger != trigger)
                continue;

            switch (active.targetType)
            {
                case TargetType.Self:
                    ApplyEffect(unit, effect, null, false);
                    break;

                case TargetType.AllAllies:
                case TargetType.AreaAlly:
                    if (allies == null) break;
                    foreach (UnitController ally in allies)
                    {
                        if (active.targetType == TargetType.AreaAlly && !IsAllyInEffectRange(unit, ally, effect, active.radius))
                            continue;

                        ApplyEffectToAlly(unit, ally, effect);
                    }
                    break;

                case TargetType.SingleAlly:
                    ApplyEffectToAlly(unit, unit, effect);
                    break;

                case TargetType.SingleEnemy:
                    ApplyEffect(unit, effect, unit.GetCurrentTarget(), false);
                    break;

                case TargetType.MultiEnemy:
                case TargetType.AreaEnemy:
                    if (monsters == null) break;
                    foreach (MonsterController monster in monsters)
                    {
                        if (!IsMonsterInEffectRange(unit, monster, effect, active.radius))
                            continue;

                        ApplyEffect(unit, effect, monster, false);
                    }
                    break;
            }
        }
    }

    private static void ApplyEffect(UnitController sourceUnit, SkillEffectData effect, MonsterController targetMonster, bool asRuntimeBuff)
    {
        if (sourceUnit == null || effect == null)
            return;

        switch (effect.effectType)
        {
            case SkillEffectType.Buff:
                sourceUnit.ApplyExtendedBuff(effect.buffType, effect.value, effect.duration, sourceUnit, asRuntimeBuff);
                break;

            case SkillEffectType.Debuff:
                if (targetMonster != null)
                    MonsterEffectHandler.ApplyEffectDebuff(sourceUnit, targetMonster, effect);
                break;

            case SkillEffectType.Damage:
                if (targetMonster != null)
                    DamageSystem.DealDamage(sourceUnit, targetMonster, GetSkillDamage(sourceUnit, effect.value), sourceUnit.Data.damageType);
                break;

            case SkillEffectType.Execute:
                if (targetMonster != null && targetMonster.monsterType != MonsterType.Boss && targetMonster.GetHpPercent() <= effect.value)
                    DamageSystem.DealDamage(sourceUnit, targetMonster, targetMonster.CurrentHp, sourceUnit.Data.damageType);
                break;

            case SkillEffectType.AddPassiveStack:
                sourceUnit.AddPassiveStack(effect.stackAmount);
                break;
        }
    }

    private static void ApplyEffectToAlly(UnitController sourceUnit, UnitController ally, SkillEffectData effect)
    {
        if (sourceUnit == null || ally == null || effect == null)
            return;

        if (effect.effectType != SkillEffectType.Buff)
            return;

        ally.ApplyExtendedBuff(effect.buffType, effect.value, effect.duration, sourceUnit, false);
    }

    private static bool IsMonsterInEffectRange(UnitController unit, MonsterController monster, SkillEffectData effect, float fallbackRadius)
    {
        if (unit == null || monster == null || !monster.IsAlive)
            return false;

        float radius = effect != null && effect.radius > 0f ? effect.radius : fallbackRadius;
        if (radius <= 0f)
            radius = unit.CurrentAttackRange;

        return Vector3.Distance(unit.transform.position, monster.transform.position) <= radius;
    }

    private static bool IsAllyInEffectRange(UnitController unit, UnitController ally, SkillEffectData effect, float fallbackRadius)
    {
        if (unit == null || ally == null)
            return false;

        float radius = effect != null && effect.radius > 0f ? effect.radius : fallbackRadius;
        if (radius <= 0f)
            return true;

        return Vector3.Distance(unit.transform.position, ally.transform.position) <= radius;
    }

    private static bool HasEffects(System.Collections.Generic.List<SkillEffectData> effects)
    {
        return effects != null && effects.Count > 0;
    }

    private static bool HasEffects(System.Collections.Generic.List<SkillEffectData> effects, SkillEffectTrigger trigger)
    {
        if (effects == null)
            return false;

        foreach (SkillEffectData effect in effects)
        {
            if (effect != null && effect.trigger == trigger)
                return true;
        }

        return false;
    }

    private static SkillEffectData GetFirstEffect(System.Collections.Generic.List<SkillEffectData> effects, SkillEffectTrigger trigger)
    {
        if (effects == null)
            return null;

        foreach (SkillEffectData effect in effects)
        {
            if (effect != null && effect.trigger == trigger)
                return effect;
        }

        return null;
    }

    private static float GetSkillDamage(UnitController unit, float multiplier)
    {
        if (unit == null)
            return 0f;

        float finalMultiplier = Mathf.Max(1f, multiplier);

        if (GameModifierState.IsEvolutionGrade(unit.Data))
            finalMultiplier *= 1f + GameModifierState.AngelDemonSkillDamageBonus;

        return unit.CurrentAttackPower * finalMultiplier;
    }
}
