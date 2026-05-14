using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnitSkillHandler
{
    private const int ConeIndicatorSegmentCount = 24;
    private const float MinimumVisibleIndicatorAlpha = 0.35f;
    private const float MinimumVisibleIndicatorDuration = 0.35f;

    private static Sprite squareSprite;

    public static void UpdateContinuousEffects(UnitController unit)
    {
        if (!IsValidUnit(unit))
            return;

        unit.TickSkillCooldown(Time.deltaTime);

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        UpdatePassiveStackFromEnemiesInAttackRange(unit, skill);
        UpdatePassiveStackFromSlowOrStunnedEnemies(unit, skill);
        ExecuteLowHpTargetsInRange(unit, skill);
        ApplyPassiveDebuffToTargetsInRange(unit, skill);

        if (skill.triggerType != SkillTriggerType.Cooldown)
            return;

        if (!unit.IsSkillCooldownReady())
            return;

        float triggerRange = GetSkillRange(unit, skill);
        MonsterController triggerTarget = ResolveTriggerTarget(unit, skill, triggerRange);
        if (triggerTarget == null && !CanExecuteWithoutTriggerTarget(skill))
            return;

        TryExecuteSkill(unit, skill, triggerTarget);
    }

    public static void ApplyPassiveOnStart(UnitController unit)
    {
    }

    public static void ApplyPassiveStatModifier(UnitController unit)
    {
    }

    public static void OnBasicAttack(UnitController unit, MonsterController target)
    {
        if (!IsValidUnit(unit))
            return;

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        ApplyPassiveStackOnBasicAttack(unit, skill);

        if (skill.triggerType == SkillTriggerType.Cooldown)
            return;

        unit.AddSkillBasicAttackCount(1);

        if (!unit.IsSkillCooldownReady())
            return;

        switch (skill.triggerType)
        {
            case SkillTriggerType.BasicAttackChance:
                if (Random.value <= Mathf.Clamp01(skill.triggerChance)
                    && TryExecuteSkill(unit, skill, target))
                {
                    unit.ResetSkillBasicAttackCount();
                }
                break;

            case SkillTriggerType.BasicAttackCount:
                if (unit.GetSkillBasicAttackCount() >= Mathf.Max(1, skill.requiredBasicAttackCount)
                    && TryExecuteSkill(unit, skill, target))
                {
                    unit.ResetSkillBasicAttackCount();
                }
                break;
        }
    }

    public static void OnBasicAttackImpact(UnitController unit, Vector3 impactPosition)
    {
        if (!IsValidUnit(unit))
            return;

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        LeviathanDeepSeaArea.TryCreate(unit, skill, impactPosition);
    }

    public static void OnMonsterKilled(UnitController unit, MonsterController target)
    {
        if (!IsValidUnit(unit))
            return;

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        int stackGain = GetPassiveStackGainOnKill(skill, target);
        if (stackGain <= 0)
            return;

        unit.AddPassiveStack(stackGain);
        unit.RecalculateStats();
    }

    public static void OnAnyMonsterKilled(MonsterController target)
    {
        if (target == null)
            return;

        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        foreach (UnitController unit in units)
        {
            if (!IsValidUnit(unit))
                continue;

            SkillData skill = unit.GetSkillData();
            if (!IsUsableSkill(skill))
                continue;

            int stackGain = GetPassiveStackGainOnAnyMonsterDeath(skill, target);
            if (stackGain <= 0)
                continue;

            unit.AddPassiveStack(stackGain);
            unit.RecalculateStats();
        }
    }

    public static void ExecuteActiveSkill(UnitController unit)
    {
        if (!IsValidUnit(unit))
            return;

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        if (!unit.IsSkillCooldownReady())
            return;

        float triggerRange = GetSkillRange(unit, skill);
        MonsterController triggerTarget = ResolveTriggerTarget(unit, skill, triggerRange);
        if (triggerTarget == null && !CanExecuteWithoutTriggerTarget(skill))
            return;

        TryExecuteSkill(unit, skill, triggerTarget);
    }

    private static bool TryExecuteSkill(UnitController unit, SkillData skill, MonsterController preferredTarget)
    {
        SkillEffectType effectType = GetEffectiveSkillEffectType(skill);

        if (effectType == SkillEffectType.ApplyBuff)
        {
            bool appliedBuff = ApplySkillBuffEffects(unit, skill);
            bool activatedSelfBuff = TryActivateSelfSkillBuff(unit, skill);
            bool activatedDeepSeaExplosion = LeviathanDeepSeaArea.TryStartExplosionOnActiveAreas(unit, skill);
            TryGainPassiveStackOnBuffSkill(unit, skill, appliedBuff);
            bool activatedAnyEffect = appliedBuff || activatedSelfBuff || activatedDeepSeaExplosion;
            if (activatedAnyEffect)
                StartExecutedSkillCooldownAndLock(unit, skill);

            return activatedAnyEffect;
        }

        if (effectType == SkillEffectType.HorizontalLineDamage)
        {
            if (!TryDealHorizontalLineDamage(unit, skill, preferredTarget, CalculateFinalSkillDamage(unit, skill)))
                return false;

            StartExecutedSkillCooldownAndLock(unit, skill);
            return true;
        }

        if (effectType == SkillEffectType.RepeatedAreaDamage)
        {
            if (!TryStartRepeatedAreaDamage(unit, skill, preferredTarget, CalculateFinalSkillDamage(unit, skill)))
                return false;

            StartExecutedSkillCooldownAndLock(unit, skill);
            return true;
        }

        if (effectType == SkillEffectType.ConeDamage)
        {
            if (!TryDealConeDamage(unit, skill, preferredTarget, CalculateFinalSkillDamage(unit, skill)))
                return false;

            StartExecutedSkillCooldownAndLock(unit, skill);
            return true;
        }

        if (skill.activeHitsAllTargetsInRange)
        {
            if (!TryDealDamageToAllTargetsInRange(unit, skill, CalculateFinalSkillDamage(unit, skill)))
                return false;

            StartExecutedSkillCooldownAndLock(unit, skill);
            return true;
        }

        MonsterController target = ResolveTarget(unit, skill, preferredTarget);
        if (target == null)
            return false;

        double finalDamage = CalculateFinalSkillDamage(unit, skill);
        float areaRadius = GetSkillAreaRadius(unit, skill);
        bool isAreaSkill = areaRadius > 0f;

        if (skill.useProjectile)
        {
            BasicAttackProjectile.SpawnSkill(
                unit,
                target,
                finalDamage,
                isAreaSkill,
                areaRadius,
                skill);
        }
        else if (isAreaSkill)
        {
            List<MonsterController> areaTargets = FindAreaSkillTargets(target.transform.position, areaRadius, skill.maxAreaTargets);
            DealAreaSkillDamage(unit, areaTargets, finalDamage);
            ApplySkillDebuffsToTargets(unit, skill, areaTargets);
        }
        else
        {
            DamageSystem.DealDamage(unit, target, finalDamage);
            ApplySkillDebuffsToTarget(unit, skill, target);
        }

        StartExecutedSkillCooldownAndLock(unit, skill);
        return true;
    }

    private static void StartExecutedSkillCooldownAndLock(UnitController unit, SkillData skill)
    {
        UpdatePassiveStackFromSlowOrStunnedEnemies(unit, skill);
        unit.StartSkillCooldown(CalculateFinalCooldown(unit, skill));
        unit.StartSkillAttackLock(GetAttackLockDurationOnCast(unit, skill));
    }

    private static float GetAttackLockDurationOnCast(UnitController unit, SkillData skill)
    {
        if (unit != null && unit.Data != null && unit.Data.skillAttackLockDurationOverride >= 0f)
            return unit.Data.skillAttackLockDurationOverride;

        return skill != null ? skill.attackLockDurationOnCast : 0f;
    }

    public static float GetGlobalPassiveAttackPowerBonus()
    {
        return GetGlobalPassiveBonus(skill => skill.passiveAllUnitAttackPowerBonus);
    }

    public static float GetGlobalPassiveAttackSpeedBonus()
    {
        return GetGlobalPassiveBonus(skill => skill.passiveAllUnitAttackSpeedBonus);
    }

    public static float GetGlobalPassiveMonsterMoveSpeedReduction()
    {
        Dictionary<SkillData, int> appliedSkillCounts = new();
        float totalReduction = 0f;
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        foreach (UnitController unit in units)
        {
            if (!IsValidUnit(unit))
                continue;

            SkillData skill = unit.GetSkillData();
            if (!IsUsableSkill(skill) || !skill.hasPassiveAura || skill.passiveAllMonsterMoveSpeedReduction <= 0f)
                continue;

            int maxSameSkillCount = Mathf.Max(1, skill.passiveAllMonsterMoveSpeedReductionMaxSameSkillCount);
            appliedSkillCounts.TryGetValue(skill, out int appliedCount);
            if (appliedCount >= maxSameSkillCount)
                continue;

            appliedSkillCounts[skill] = appliedCount + 1;
            totalReduction += skill.passiveAllMonsterMoveSpeedReduction;
        }

        return Mathf.Clamp01(totalReduction);
    }

    public static float GetSelfPassiveAttackPowerBonus(UnitController unit)
    {
        SkillData skill = GetSelfPassiveSkill(unit);
        return skill != null ? skill.passiveSelfAttackPowerBonus : 0f;
    }

    public static float GetSelfPassiveAttackSpeedBonus(UnitController unit)
    {
        SkillData skill = GetSelfPassiveSkill(unit);
        return skill != null ? skill.passiveSelfAttackSpeedBonus : 0f;
    }

    public static float GetPassiveStackAttackPowerBonus(UnitController unit)
    {
        SkillData skill = GetSelfPassiveSkill(unit);
        return skill != null ? unit.GetPassiveStack() * skill.attackPowerBonusPerPassiveStack : 0f;
    }

    public static float GetPassiveStackAttackSpeedBonus(UnitController unit)
    {
        SkillData skill = GetSelfPassiveSkill(unit);
        if (skill == null)
            return 0f;

        float bonus = unit.GetPassiveStack() * skill.attackSpeedBonusPerPassiveStack;
        if (skill.maxPassiveStack > 0
            && unit.GetPassiveStack() >= skill.maxPassiveStack
            && skill.passiveMaxStackAttackSpeedBonus > 0f)
        {
            bonus += skill.passiveMaxStackAttackSpeedBonus;
        }

        return bonus;
    }

    public static float GetPassiveDamageMultiplier(UnitController unit)
    {
        SkillData skill = GetSelfPassiveSkill(unit);
        if (skill == null || skill.passiveDamageBonusPerEnemyInRange <= 0f)
            return 1f;

        int enemyCount = unit.GetTargetsInRangeCount();
        if (skill.passiveDamageBonusMaxEnemyCount > 0)
            enemyCount = Mathf.Min(enemyCount, skill.passiveDamageBonusMaxEnemyCount);

        return 1f + Mathf.Max(0f, skill.passiveDamageBonusPerEnemyInRange) * Mathf.Max(0, enemyCount);
    }

    public static bool ShouldExecuteLowHpTarget(UnitController unit, MonsterController target)
    {
        if (target == null || !target.IsAlive)
            return false;

        if (target.isBoss || target.monsterType != MonsterType.Normal)
            return false;

        SkillData skill = GetSelfPassiveSkill(unit);
        if (skill == null || skill.passiveExecuteHpPercent <= 0f)
            return false;

        return target.GetHpPercent() <= Mathf.Clamp01(skill.passiveExecuteHpPercent);
    }

    private static void ExecuteLowHpTargetsInRange(UnitController unit, SkillData skill)
    {
        if (skill.passiveExecuteHpPercent <= 0f)
            return;

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive || monster.isBoss || monster.monsterType != MonsterType.Normal)
                continue;

            if (monster.GetHpPercent() > Mathf.Clamp01(skill.passiveExecuteHpPercent))
                continue;

            if (Vector3.Distance(unit.transform.position, monster.transform.position) > unit.CurrentAttackRange)
                continue;

            DamageSystem.DealDamage(unit, monster, monster.CurrentHp);
        }
    }

    private static void ApplyPassiveDebuffToTargetsInRange(UnitController unit, SkillData skill)
    {
        if (!skill.hasPassiveAura || skill.passiveDebuffType == DebuffType.None)
            return;

        float radius = Mathf.Max(0f, skill.passiveDebuffRadius);
        if (radius <= 0f)
            return;

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (Vector3.Distance(unit.transform.position, monster.transform.position) > radius)
                continue;

            ApplyDebuffToTarget(
                unit,
                monster,
                skill.passiveDebuffType,
                skill.passiveDebuffValue,
                skill.passiveDebuffDuration,
                1,
                1);
        }
    }

    private static void UpdatePassiveStackFromEnemiesInAttackRange(UnitController unit, SkillData skill)
    {
        if (!skill.hasPassiveAura || !skill.passiveStackTracksEnemiesInAttackRange)
            return;

        int enemyCount = unit.GetTargetsInRangeCount();
        if (skill.maxPassiveStack > 0)
            enemyCount = Mathf.Min(enemyCount, skill.maxPassiveStack);

        if (unit.GetPassiveStack() == enemyCount)
            return;

        unit.SetPassiveStack(enemyCount);
        unit.RecalculateStats();
    }

    private static void UpdatePassiveStackFromSlowOrStunnedEnemies(UnitController unit, SkillData skill)
    {
        if (!skill.hasPassiveAura || !skill.passiveStackTracksSlowOrStunnedEnemies)
            return;

        int enemyCount = unit.CountEnemiesWithDebuffs(DebuffType.Slow, DebuffType.Stun);
        if (skill.maxPassiveStack > 0)
            enemyCount = Mathf.Min(enemyCount, skill.maxPassiveStack);

        if (unit.GetPassiveStack() == enemyCount)
            return;

        unit.SetPassiveStack(enemyCount);
        unit.RecalculateStats();
    }

    private static int GetPassiveStackGainOnKill(SkillData skill, MonsterController target)
    {
        if (skill == null || target == null)
            return 0;

        bool isBossOrElite = target.isBoss || target.monsterType != MonsterType.Normal;
        if (isBossOrElite && skill.passiveBossOrEliteStackGainOnKill > 0)
            return skill.passiveBossOrEliteStackGainOnKill;

        return skill.passiveStackGainOnKill;
    }

    private static int GetPassiveStackGainOnAnyMonsterDeath(SkillData skill, MonsterController target)
    {
        if (skill == null || target == null)
            return 0;

        bool isBossOrElite = target.isBoss || target.monsterType != MonsterType.Normal;
        if (isBossOrElite && skill.passiveBossOrEliteStackGainOnAnyMonsterDeath > 0)
            return skill.passiveBossOrEliteStackGainOnAnyMonsterDeath;

        return skill.passiveStackGainOnAnyMonsterDeath;
    }

    private static bool ApplySkillBuffEffects(UnitController caster, SkillData skill)
    {
        if (skill.buffEffects == null || skill.buffEffects.Length == 0)
            return false;

        bool appliedAnyBuff = false;

        foreach (SkillBuffEffect buffEffect in skill.buffEffects)
        {
            if (buffEffect == null)
                continue;

            if (buffEffect.applyToAllUnits)
            {
                appliedAnyBuff |= ApplyBuffToAllUnits(caster, buffEffect);
            }
            else
            {
                appliedAnyBuff |= ApplyBuffToUnit(caster, caster, buffEffect);
            }
        }

        return appliedAnyBuff;
    }

    private static bool TryActivateSelfSkillBuff(UnitController unit, SkillData skill)
    {
        if (!IsValidUnit(unit) || skill == null)
            return false;

        if (skill.activeBuffDuration <= 0f)
            return false;

        bool hasActiveSelfEffect = skill.activeSelfAttackPowerBonus > 0f
            || skill.activeSelfRangeOverride > 0f
            || skill.activeAttackPowerBonusPerEnemyInRange > 0f;

        if (!hasActiveSelfEffect)
            return false;

        unit.StartActiveSelfSkillBuff(skill.activeBuffDuration);
        return true;
    }

    private static void ApplyPassiveStackOnBasicAttack(UnitController unit, SkillData skill)
    {
        if (skill.passiveStackGainOnBasicAttack <= 0)
            return;

        unit.AddPassiveStack(skill.passiveStackGainOnBasicAttack);
        unit.RecalculateStats();
    }

    private static bool ApplyBuffToAllUnits(UnitController caster, SkillBuffEffect buffEffect)
    {
        bool appliedAnyBuff = false;
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        foreach (UnitController unit in units)
            appliedAnyBuff |= ApplyBuffToUnit(unit, caster, buffEffect);

        return appliedAnyBuff;
    }

    private static bool ApplyBuffToUnit(UnitController targetUnit, UnitController sourceUnit, SkillBuffEffect buffEffect)
    {
        if (!IsValidUnit(targetUnit))
            return false;

        targetUnit.ApplyExtendedBuff(
            buffEffect.buffType,
            buffEffect.value,
            Mathf.Max(0f, buffEffect.duration),
            sourceUnit,
            true);

        targetUnit.RecalculateStats();
        return true;
    }

    private static void TryGainPassiveStackOnBuffSkill(UnitController unit, SkillData skill, bool appliedBuff)
    {
        if (!appliedBuff || !IsValidUnit(unit) || skill == null)
            return;

        if (!skill.hasPassiveAura || skill.passiveStackGainOnBuffSkill <= 0)
            return;

        if (unit.IsPassiveMaxStackBuffActive())
            return;

        unit.AddPassiveStack(skill.passiveStackGainOnBuffSkill);
        unit.RecalculateStats();

        if (skill.maxPassiveStack <= 0 || unit.GetPassiveStack() < skill.maxPassiveStack)
            return;

        TryApplyPassiveMaxStackBuff(unit, skill);
    }

    private static void TryApplyPassiveMaxStackBuff(UnitController unit, SkillData skill)
    {
        if (skill.passiveMaxStackBuffEffects == null || skill.passiveMaxStackBuffEffects.Length == 0)
            return;

        unit.SetPassiveMaxStackBuffActive(true);

        float longestDuration = 0f;
        foreach (SkillBuffEffect buffEffect in skill.passiveMaxStackBuffEffects)
        {
            if (buffEffect == null)
                continue;

            longestDuration = Mathf.Max(longestDuration, Mathf.Max(0f, buffEffect.duration));

            if (buffEffect.applyToAllUnits)
                ApplyBuffToAllUnits(unit, buffEffect);
            else
                ApplyBuffToUnit(unit, unit, buffEffect);
        }

        unit.StartCoroutine(CoResetPassiveStackAfterMaxStackBuff(unit, Mathf.Max(0f, longestDuration)));
    }

    private static IEnumerator CoResetPassiveStackAfterMaxStackBuff(UnitController unit, float duration)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        if (!IsValidUnit(unit))
            yield break;

        unit.ResetPassiveStack();
        unit.SetPassiveMaxStackBuffActive(false);
        unit.RecalculateStats();
    }

    private static MonsterController ResolveTarget(UnitController unit, SkillData skill, MonsterController preferredTarget)
    {
        float range = GetSkillRange(unit, skill);

        if (skill.useCurrentTargetFirst && IsValidTarget(unit, preferredTarget, range))
            return preferredTarget;

        return UnitTargetFinder.FindTarget(
            unit.transform.position,
            range,
            skill.targetPriority,
            skill.bossFallbackTargetPriority);
    }

    private static MonsterController ResolveTriggerTarget(UnitController unit, SkillData skill, float range)
    {
        MonsterController currentTarget = unit.GetCurrentTarget();
        if (IsValidTarget(unit, currentTarget, range))
            return currentTarget;

        return UnitTargetFinder.FindTarget(
            unit.transform.position,
            range,
            skill.targetPriority,
            skill.bossFallbackTargetPriority);
    }

    private static double CalculateFinalSkillDamage(UnitController unit, SkillData skill)
    {
        UnitGrowthEntry growth = GetUnitGrowth(unit);
        float skillDamageMultiplier = UnitStatCalculator.GetSkillDamageMultiplier(growth);

        if (GameModifierState.IsEvolutionGrade(unit.Data))
            skillDamageMultiplier += GameModifierState.AngelDemonSkillDamageBonus;

        double baseSkillDamage = unit.CurrentAttackPower * Mathf.Max(0f, skill.baseSkillDamageMultiplier);
        double additionalSkillDamage = Mathf.Max(0f, skill.additionalSkillDamage);
        return (baseSkillDamage + additionalSkillDamage) * Mathf.Max(0f, skillDamageMultiplier);
    }

    public static float CalculateFinalCooldown(UnitController unit, SkillData skill)
    {
        UnitGrowthEntry growth = GetUnitGrowth(unit);
        float cooldownReduction = UnitStatCalculator.GetSkillCooldownReduction(growth);

        if (GameModifierState.IsEvolutionGrade(unit.Data))
            cooldownReduction += GameModifierState.AngelDemonCooldownReduction;

        float cooldown = skill.cooldown * (1f - Mathf.Clamp01(cooldownReduction));
        cooldown -= unit.GetPassiveStack() * Mathf.Max(0f, skill.cooldownReductionPerPassiveStack);
        return Mathf.Max(0f, cooldown);
    }

    private static bool TryDealDamageToAllTargetsInRange(UnitController unit, SkillData skill, double finalDamage)
    {
        if (!IsValidUnit(unit) || skill == null)
            return false;

        float range = GetSkillRange(unit, skill);
        bool hitAnyTarget = false;
        List<MonsterController> targets = FindActiveSkillTargetsInRange(unit, skill, range);

        foreach (MonsterController monster in targets)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (finalDamage > 0.0)
                DamageSystem.DealDamage(unit, monster, finalDamage);

            ApplyCorruptionLordOnActiveSkillHit(unit, skill, monster);
            ApplySkillDebuffsToTarget(unit, skill, monster);
            hitAnyTarget = true;
        }

        return hitAnyTarget;
    }

    private static List<MonsterController> FindActiveSkillTargetsInRange(UnitController unit, SkillData skill, float range)
    {
        List<MonsterController> targets = new();
        if (unit == null || skill == null)
            return targets;

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!skill.activeTargetsEntireField
                && Vector3.Distance(unit.transform.position, monster.transform.position) > range)
            {
                continue;
            }

            targets.Add(monster);
        }

        targets.Sort((a, b) =>
            Vector3.Distance(unit.transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(unit.transform.position, b.transform.position)));

        if (skill.maxAreaTargets > 0 && targets.Count > skill.maxAreaTargets)
            targets.RemoveRange(skill.maxAreaTargets, targets.Count - skill.maxAreaTargets);

        return targets;
    }

    private static void DealAreaSkillDamage(UnitController unit, Vector3 center, float radius, double finalDamage)
    {
        DealAreaSkillDamage(unit, FindAreaSkillTargets(center, radius, 0), finalDamage);
    }

    private static void DealAreaSkillDamage(UnitController unit, List<MonsterController> targets, double finalDamage)
    {
        if (targets == null)
            return;

        foreach (MonsterController monster in targets)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            DamageSystem.DealDamage(unit, monster, finalDamage);
        }
    }

    public static void ApplySkillDebuffsInArea(UnitController unit, SkillData skill, Vector3 center, float radius)
    {
        if (!HasSkillDebuffs(skill))
            return;

        ApplySkillDebuffsToTargets(unit, skill, FindAreaSkillTargets(center, radius, skill.maxAreaTargets));
    }

    private static void ApplySkillDebuffsToTargets(UnitController unit, SkillData skill, List<MonsterController> targets)
    {
        if (!HasSkillDebuffs(skill) || targets == null)
            return;

        foreach (MonsterController monster in targets)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            ApplySkillDebuffsToTarget(unit, skill, monster);
        }
    }

    private static List<MonsterController> FindAreaSkillTargets(Vector3 center, float radius, int maxTargets)
    {
        List<MonsterController> targets = new();
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (Vector3.Distance(center, monster.transform.position) <= radius)
                targets.Add(monster);
        }

        targets.Sort((a, b) =>
            Vector3.Distance(center, a.transform.position)
            .CompareTo(Vector3.Distance(center, b.transform.position)));

        if (maxTargets > 0 && targets.Count > maxTargets)
            targets.RemoveRange(maxTargets, targets.Count - maxTargets);

        return targets;
    }

    public static void ApplySkillDebuffsToTarget(UnitController unit, SkillData skill, MonsterController target)
    {
        if (!HasSkillDebuffs(skill) || target == null || !target.IsAlive)
            return;

        foreach (SkillDebuffEffect debuffEffect in skill.debuffEffects)
        {
            if (debuffEffect == null || debuffEffect.debuffType == DebuffType.None)
                continue;

            ApplyDebuffToTarget(
                unit,
                target,
                debuffEffect.debuffType,
                debuffEffect.value,
                debuffEffect.duration,
                debuffEffect.stack,
                debuffEffect.maxStack,
                debuffEffect.damageMultiplierOnExpire,
                debuffEffect.currentHpDamagePercentOnExpire);
        }
    }

    public static void ApplyCorruptionLordOnBasicAttack(UnitController unit, MonsterController target)
    {
        SkillData skill = GetSelfPassiveSkill(unit);
        if (skill == null || !skill.hasCorruptionLord || target == null || !target.IsAlive)
            return;

        TryApplyCorruptionLord(unit, skill, target);
    }

    private static void ApplyCorruptionLordOnActiveSkillHit(UnitController unit, SkillData skill, MonsterController target)
    {
        if (!IsValidUnit(unit) || skill == null || !skill.hasCorruptionLord || target == null || !target.IsAlive)
            return;

        if (target.HasDebuff(DebuffType.CorruptionLord))
        {
            double bonusDamage = target.MaxHp * Mathf.Max(0f, skill.corruptionLordActiveBonusMaxHpDamagePercent);
            DamageSystem.DealRawDamage(unit, target, bonusDamage);
            return;
        }

        TryApplyCorruptionLord(unit, skill, target);
    }

    private static bool TryApplyCorruptionLord(UnitController unit, SkillData skill, MonsterController target)
    {
        if (!IsValidUnit(unit) || skill == null || target == null || !target.IsAlive)
            return false;

        if (target.HasDebuff(DebuffType.CorruptionLord))
            return false;

        float duration = Mathf.Max(0.01f, skill.corruptionLordDuration);
        float interval = Mathf.Max(0.01f, skill.corruptionLordTickInterval);

        target.AddDebuff(new DebuffInstance
        {
            debuffType = DebuffType.CorruptionLord,
            value = 0f,
            duration = duration,
            remainTime = duration,
            stack = 1,
            maxStack = 1,
            source = unit,
            maxHpDamagePercentPerTick = Mathf.Max(0f, skill.corruptionLordMaxHpDamagePercentPerTick),
            tickInterval = interval,
            tickTimer = interval
        });

        return true;
    }

    private static void ApplyDebuffToTarget(
        UnitController unit,
        MonsterController target,
        DebuffType debuffType,
        float value,
        float duration,
        int stack,
        int maxStack,
        float damageMultiplierOnExpire = 0f,
        float currentHpDamagePercentOnExpire = 0f)
    {
        if (unit == null || target == null || !target.IsAlive || debuffType == DebuffType.None)
            return;

        target.AddDebuff(new DebuffInstance
        {
            debuffType = debuffType,
            value = Mathf.Max(0f, value),
            duration = Mathf.Max(0.01f, duration),
            remainTime = Mathf.Max(0.01f, duration),
            stack = Mathf.Max(1, stack),
            maxStack = Mathf.Max(1, maxStack),
            source = unit,
            damageMultiplierOnExpire = Mathf.Max(0f, damageMultiplierOnExpire),
            currentHpDamagePercentOnExpire = Mathf.Max(0f, currentHpDamagePercentOnExpire)
        });
    }

    private static bool HasSkillDebuffs(SkillData skill)
    {
        return skill != null && skill.debuffEffects != null && skill.debuffEffects.Length > 0;
    }

    private static bool TryDealHorizontalLineDamage(UnitController unit, SkillData skill, MonsterController preferredTarget, double finalDamage)
    {
        if (skill.lineOrigin == SkillLineOrigin.TargetPosition)
        {
            MonsterController target = ResolveTarget(unit, skill, preferredTarget);
            if (target == null)
                return false;

            if (skill.useProjectile)
            {
                BasicAttackProjectile.SpawnHorizontalLineSkill(unit, target, finalDamage, skill);
                return true;
            }

            DealHorizontalLineDamageAt(unit, skill, target.transform.position, finalDamage);
            return true;
        }

        DealHorizontalLineDamageAt(unit, skill, unit.transform.position, finalDamage, preferredTarget);
        return true;
    }

    private static bool TryStartRepeatedAreaDamage(UnitController unit, SkillData skill, MonsterController preferredTarget, double finalDamage)
    {
        Vector3 center;

        if (skill.repeatedDamageOrigin == SkillLineOrigin.TargetPosition)
        {
            MonsterController target = ResolveTarget(unit, skill, preferredTarget);
            if (target == null)
                return false;

            center = target.transform.position;
        }
        else
        {
            center = unit.transform.position;
        }

        unit.StartCoroutine(CoRepeatedAreaDamage(unit, skill, center, finalDamage));
        return true;
    }

    private static bool TryDealConeDamage(UnitController unit, SkillData skill, MonsterController preferredTarget, double finalDamage)
    {
        if (!IsValidUnit(unit) || skill == null)
            return false;

        float range = Mathf.Max(0f, skill.coneRange);
        float halfAngle = Mathf.Clamp(skill.coneAngle, 1f, 360f) * 0.5f;
        if (range <= 0f)
            return false;

        Vector3 forward = ResolveConeForward(unit, skill, preferredTarget, range);
        if (skill.showAreaAttackIndicator)
            SpawnConeIndicator(unit.transform.position, forward, range, halfAngle, skill.areaAttackIndicatorColor, skill.areaAttackIndicatorDuration);

        bool hitAnyTarget = false;
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            Vector3 offset = monster.transform.position - unit.transform.position;
            if (offset.sqrMagnitude > range * range)
                continue;

            if (offset.sqrMagnitude <= Mathf.Epsilon)
                continue;

            if (Vector3.Angle(forward, offset.normalized) > halfAngle)
                continue;

            DamageSystem.DealDamage(unit, monster, finalDamage);
            ApplySkillDebuffsToTarget(unit, skill, monster);
            hitAnyTarget = true;
        }

        return hitAnyTarget;
    }

    private static Vector3 ResolveConeForward(UnitController unit, SkillData skill, MonsterController preferredTarget, float range)
    {
        if (skill.aimConeAtTarget && IsValidTarget(unit, preferredTarget, range))
            return (preferredTarget.transform.position - unit.transform.position).normalized;

        if (skill.aimConeAtTarget)
        {
            MonsterController target = UnitTargetFinder.FindTarget(
                unit.transform.position,
                range,
                skill.targetPriority,
                skill.bossFallbackTargetPriority);

            if (target != null)
                return (target.transform.position - unit.transform.position).normalized;
        }

        switch (skill.coneFallbackDirection)
        {
            case SkillLineDirection.Left:
                return Vector3.left;

            case SkillLineDirection.Right:
                return Vector3.right;

            default:
                return Vector3.right;
        }
    }

    private static IEnumerator CoRepeatedAreaDamage(UnitController unit, SkillData skill, Vector3 center, double finalDamage)
    {
        int hitCount = Mathf.Max(1, skill.repeatedHitCount);
        float interval = Mathf.Max(0.01f, skill.repeatedHitInterval);
        GameObject areaIndicator = null;

        float areaRadius = GetSkillAreaRadius(unit, skill);
        if (skill.showAreaAttackIndicator && areaRadius > 0f)
        {
            areaIndicator = BasicAttackProjectile.SpawnAreaIndicator(
                center,
                areaRadius,
                skill.areaAttackIndicatorColor,
                skill.areaAttackIndicatorDuration,
                false);
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (!IsValidUnit(unit))
            {
                if (areaIndicator != null)
                    Object.Destroy(areaIndicator);

                yield break;
            }

            DealAreaSkillDamage(unit, FindAreaSkillTargets(center, areaRadius, skill.maxAreaTargets), finalDamage);

            yield return new WaitForSeconds(interval);
        }

        if (areaIndicator != null)
            Object.Destroy(areaIndicator);
    }

    public static void DealHorizontalLineDamageAt(UnitController unit, SkillData skill, Vector3 origin, double finalDamage)
    {
        DealHorizontalLineDamageAt(unit, skill, origin, finalDamage, null);
    }

    private static void DealHorizontalLineDamageAt(UnitController unit, SkillData skill, Vector3 origin, double finalDamage, MonsterController preferredTarget)
    {
        if (!IsValidUnit(unit) || skill == null)
            return;

        float length = Mathf.Max(0f, skill.lineLength);
        float halfWidth = Mathf.Max(0f, skill.lineWidth) * 0.5f;

        if (length <= 0f || halfWidth <= 0f)
            return;

        Vector3 lineForward = ResolveLineForward(unit, skill, origin, preferredTarget);

        if (skill.showLineIndicator)
            SpawnLineIndicator(origin, skill, lineForward);

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            Vector3 offset = monster.transform.position - origin;
            if (!IsInsideLine(offset, length, halfWidth, lineForward, skill.lineDirection))
                continue;

            DamageSystem.DealDamage(unit, monster, finalDamage);
        }
    }

    private static Vector3 ResolveLineForward(UnitController unit, SkillData skill, Vector3 origin, MonsterController preferredTarget)
    {
        if (skill == null)
            return Vector3.right;

        if (skill.lineDirection == SkillLineDirection.Left)
            return Vector3.left;

        if (skill.lineDirection == SkillLineDirection.Right || skill.lineDirection == SkillLineDirection.Both)
            return Vector3.right;

        float range = GetSkillRange(unit, skill);
        MonsterController target = unit != null ? unit.GetCurrentTarget() : null;
        if (!IsValidTarget(unit, target, range))
            target = preferredTarget;

        if (!IsValidTarget(unit, target, range))
        {
            target = UnitTargetFinder.FindTarget(
                unit.transform.position,
                range,
                skill.targetPriority,
                skill.bossFallbackTargetPriority);
        }

        if (target == null)
            return Vector3.right;

        Vector3 forward = target.transform.position - origin;
        forward.z = 0f;

        if (forward.sqrMagnitude <= Mathf.Epsilon)
            return Vector3.right;

        return forward.normalized;
    }

    private static bool IsInsideLine(Vector3 offset, float length, float halfWidth, Vector3 forward, SkillLineDirection direction)
    {
        if (forward.sqrMagnitude <= Mathf.Epsilon)
            return false;

        Vector3 normalizedForward = forward.normalized;
        Vector3 normalizedRight = new Vector3(-normalizedForward.y, normalizedForward.x, 0f);
        float forwardDistance = Vector3.Dot(offset, normalizedForward);
        float sideDistance = Vector3.Dot(offset, normalizedRight);

        if (Mathf.Abs(sideDistance) > halfWidth)
            return false;

        if (direction == SkillLineDirection.Both)
            return Mathf.Abs(forwardDistance) <= length;

        return forwardDistance >= 0f && forwardDistance <= length;
    }

    private static void SpawnLineIndicator(Vector3 origin, SkillData skill, Vector3 forward)
    {
        GameObject indicatorObject = new GameObject("HorizontalLineSkillIndicator");
        indicatorObject.transform.position = GetLineIndicatorCenter(origin, skill, forward);
        indicatorObject.transform.rotation = GetLineIndicatorRotation(forward);
        indicatorObject.transform.localScale = GetLineIndicatorScale(skill);

        SpriteRenderer spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSquareSprite();
        spriteRenderer.color = skill.lineIndicatorColor;
        spriteRenderer.sortingOrder = 20;

        Object.Destroy(indicatorObject, Mathf.Max(0.01f, skill.lineIndicatorDuration));
    }

    private static Vector3 GetLineIndicatorCenter(Vector3 origin, SkillData skill, Vector3 forward)
    {
        float halfLength = Mathf.Max(0f, skill.lineLength) * 0.5f;
        if (skill.lineDirection == SkillLineDirection.Both)
            return origin;

        return origin + forward.normalized * halfLength;
    }

    private static Quaternion GetLineIndicatorRotation(Vector3 forward)
    {
        if (forward.sqrMagnitude <= Mathf.Epsilon)
            return Quaternion.identity;

        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    private static Vector3 GetLineIndicatorScale(SkillData skill)
    {
        float length = Mathf.Max(0.01f, skill.lineLength);
        float width = Mathf.Max(0.01f, skill.lineWidth);
        float displayLength = skill.lineDirection == SkillLineDirection.Both ? length * 2f : length;
        return new Vector3(displayLength, width, 1f);
    }

    private static void SpawnConeIndicator(Vector3 origin, Vector3 forward, float range, float halfAngle, Color color, float duration)
    {
        if (range <= 0f || forward.sqrMagnitude <= Mathf.Epsilon)
            return;

        GameObject indicatorObject = new GameObject("ConeSkillIndicator");
        indicatorObject.transform.position = origin;

        MeshFilter meshFilter = indicatorObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = indicatorObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = CreateConeMesh(forward.normalized, range, halfAngle);

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
            meshRenderer.material = new Material(spriteShader);

        if (meshRenderer.material != null)
            meshRenderer.material.color = GetVisibleIndicatorColor(color);

        meshRenderer.sortingOrder = 21;
        Object.Destroy(indicatorObject, Mathf.Max(MinimumVisibleIndicatorDuration, duration));
    }

    private static Mesh CreateConeMesh(Vector3 forward, float range, float halfAngle)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[ConeIndicatorSegmentCount + 2];
        int[] triangles = new int[ConeIndicatorSegmentCount * 3];

        vertices[0] = Vector3.zero;
        float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float startAngle = forwardAngle - halfAngle;
        float angleStep = (halfAngle * 2f) / ConeIndicatorSegmentCount;

        for (int i = 0; i <= ConeIndicatorSegmentCount; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * range, Mathf.Sin(angle) * range, 0f);
        }

        for (int i = 0; i < ConeIndicatorSegmentCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Color GetVisibleIndicatorColor(Color color)
    {
        if (color.a < MinimumVisibleIndicatorAlpha)
            color.a = MinimumVisibleIndicatorAlpha;

        return color;
    }

    private static Sprite GetSquareSprite()
    {
        if (squareSprite != null)
            return squareSprite;

        Texture2D texture = Texture2D.whiteTexture;
        squareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width);

        return squareSprite;
    }

    private static bool IsValidUnit(UnitController unit)
    {
        return unit != null && unit.Data != null;
    }

    private static bool IsUsableSkill(SkillData skill)
    {
        return skill != null && skill.isEnabled;
    }

    private static bool IsValidTarget(UnitController unit, MonsterController target, float range)
    {
        if (unit == null || target == null || !target.IsAlive)
            return false;

        return Vector3.Distance(unit.transform.position, target.transform.position) <= range;
    }

    private static float GetSkillRange(UnitController unit, SkillData skill)
    {
        if (skill == null || skill.skillRange <= 0f)
            return unit != null ? unit.CurrentAttackRange : 0f;

        return skill.skillRange;
    }

    private static float GetSkillAreaRadius(UnitController unit, SkillData skill)
    {
        if (skill == null)
            return 0f;

        if (skill.useBasicAttackRadius && unit != null && unit.Data != null)
            return Mathf.Max(0f, unit.Data.attackRadius);

        return Mathf.Max(0f, skill.areaRadius);
    }

    private static SkillEffectType GetEffectiveSkillEffectType(SkillData skill)
    {
        if (skill == null)
            return SkillEffectType.Damage;

        if (skill.effectType != SkillEffectType.Damage)
            return skill.effectType;

        if (skill.repeatedHitCount > 1 && (skill.areaRadius > 0f || skill.useBasicAttackRadius))
            return SkillEffectType.RepeatedAreaDamage;

        return skill.effectType;
    }

    private static bool CanExecuteWithoutTriggerTarget(SkillData skill)
    {
        return skill != null
            && skill.activeExplodesDeepSeaAreas
            && LeviathanDeepSeaArea.HasActiveArea();
    }

    private static UnitGrowthEntry GetUnitGrowth(UnitController unit)
    {
        if (unit == null || unit.Data == null || UnitGrowthManager.Instance == null)
            return null;

        return UnitGrowthManager.Instance.GetUnitGrowth(unit.Data.unitId);
    }

    private static SkillData GetSelfPassiveSkill(UnitController unit)
    {
        if (!IsValidUnit(unit))
            return null;

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return null;

        return skill.hasPassiveAura ? skill : null;
    }

    private static float GetGlobalPassiveBonus(System.Func<SkillData, float> selector)
    {
        float totalBonus = 0f;
        HashSet<SkillData> appliedUniqueSkills = new();
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        foreach (UnitController unit in units)
        {
            if (!IsValidUnit(unit))
                continue;

            SkillData skill = unit.GetSkillData();
            if (!IsUsableSkill(skill) || !skill.hasPassiveAura)
                continue;

            if (!skill.stackPassiveAuraWithSameSkill)
            {
                if (appliedUniqueSkills.Contains(skill))
                    continue;

                appliedUniqueSkills.Add(skill);
            }

            totalBonus += selector(skill);
        }

        return totalBonus;
    }
}
