using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnitSkillHandler
{
    private static Sprite squareSprite;

    public static void UpdateContinuousEffects(UnitController unit)
    {
        if (!IsValidUnit(unit))
            return;

        unit.TickSkillCooldown(Time.deltaTime);

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        ExecuteLowHpTargetsInRange(unit, skill);

        if (skill.triggerType != SkillTriggerType.Cooldown)
            return;

        if (!unit.IsSkillCooldownReady())
            return;

        TryExecuteSkill(unit, skill, unit.GetCurrentTarget());
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

    public static void OnMonsterKilled(UnitController unit, MonsterController target)
    {
        if (!IsValidUnit(unit))
            return;

        SkillData skill = unit.GetSkillData();
        if (!IsUsableSkill(skill))
            return;

        if (skill.passiveStackGainOnKill <= 0)
            return;

        unit.AddPassiveStack(skill.passiveStackGainOnKill);
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

        TryExecuteSkill(unit, skill, unit.GetCurrentTarget());
    }

    private static bool TryExecuteSkill(UnitController unit, SkillData skill, MonsterController preferredTarget)
    {
        if (skill.effectType == SkillEffectType.ApplyBuff)
        {
            ApplySkillBuffEffects(unit, skill);
            unit.StartSkillCooldown(CalculateFinalCooldown(unit, skill));
            return true;
        }

        if (skill.effectType == SkillEffectType.HorizontalLineDamage)
        {
            if (!TryDealHorizontalLineDamage(unit, skill, preferredTarget, CalculateFinalSkillDamage(unit, skill)))
                return false;

            unit.StartSkillCooldown(CalculateFinalCooldown(unit, skill));
            return true;
        }

        if (skill.effectType == SkillEffectType.RepeatedAreaDamage)
        {
            if (!TryStartRepeatedAreaDamage(unit, skill, preferredTarget, CalculateFinalSkillDamage(unit, skill)))
                return false;

            unit.StartSkillCooldown(CalculateFinalCooldown(unit, skill));
            return true;
        }

        MonsterController target = ResolveTarget(unit, skill, preferredTarget);
        if (target == null)
            return false;

        double finalDamage = CalculateFinalSkillDamage(unit, skill);
        bool isAreaSkill = skill.areaRadius > 0f;

        if (skill.useProjectile)
        {
            BasicAttackProjectile.SpawnSkill(
                unit,
                target,
                finalDamage,
                isAreaSkill,
                skill.areaRadius,
                skill);
        }
        else if (isAreaSkill)
        {
            DealAreaSkillDamage(unit, target.transform.position, skill.areaRadius, finalDamage);
        }
        else
        {
            DamageSystem.DealDamage(unit, target, finalDamage);
        }

        unit.StartSkillCooldown(CalculateFinalCooldown(unit, skill));
        return true;
    }

    public static float GetGlobalPassiveAttackPowerBonus()
    {
        return GetGlobalPassiveBonus(skill => skill.passiveAllUnitAttackPowerBonus);
    }

    public static float GetGlobalPassiveAttackSpeedBonus()
    {
        return GetGlobalPassiveBonus(skill => skill.passiveAllUnitAttackSpeedBonus);
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
        return skill != null ? unit.GetPassiveStack() * skill.attackSpeedBonusPerPassiveStack : 0f;
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

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

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

    private static void ApplySkillBuffEffects(UnitController caster, SkillData skill)
    {
        if (skill.buffEffects == null || skill.buffEffects.Length == 0)
            return;

        foreach (SkillBuffEffect buffEffect in skill.buffEffects)
        {
            if (buffEffect == null)
                continue;

            if (buffEffect.applyToAllUnits)
            {
                ApplyBuffToAllUnits(caster, buffEffect);
            }
            else
            {
                ApplyBuffToUnit(caster, caster, buffEffect);
            }
        }
    }

    private static void ApplyPassiveStackOnBasicAttack(UnitController unit, SkillData skill)
    {
        if (skill.passiveStackGainOnBasicAttack <= 0)
            return;

        unit.AddPassiveStack(skill.passiveStackGainOnBasicAttack);
        unit.RecalculateStats();
    }

    private static void ApplyBuffToAllUnits(UnitController caster, SkillBuffEffect buffEffect)
    {
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        foreach (UnitController unit in units)
            ApplyBuffToUnit(unit, caster, buffEffect);
    }

    private static void ApplyBuffToUnit(UnitController targetUnit, UnitController sourceUnit, SkillBuffEffect buffEffect)
    {
        if (!IsValidUnit(targetUnit))
            return;

        targetUnit.ApplyExtendedBuff(
            buffEffect.buffType,
            buffEffect.value,
            Mathf.Max(0f, buffEffect.duration),
            sourceUnit,
            true);

        targetUnit.RecalculateStats();
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

        return Mathf.Max(0f, skill.cooldown * (1f - Mathf.Clamp01(cooldownReduction)));
    }

    private static void DealAreaSkillDamage(UnitController unit, Vector3 center, float radius, double finalDamage)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (Vector3.Distance(center, monster.transform.position) <= radius)
                DamageSystem.DealDamage(unit, monster, finalDamage);
        }
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

        DealHorizontalLineDamageAt(unit, skill, unit.transform.position, finalDamage);
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

    private static IEnumerator CoRepeatedAreaDamage(UnitController unit, SkillData skill, Vector3 center, double finalDamage)
    {
        int hitCount = Mathf.Max(1, skill.repeatedHitCount);
        float interval = Mathf.Max(0.01f, skill.repeatedHitInterval);
        GameObject areaIndicator = null;

        if (skill.showAreaAttackIndicator && skill.areaRadius > 0f)
        {
            areaIndicator = BasicAttackProjectile.SpawnAreaIndicator(
                center,
                skill.areaRadius,
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

            DealAreaSkillDamage(unit, center, skill.areaRadius, finalDamage);

            yield return new WaitForSeconds(interval);
        }

        if (areaIndicator != null)
            Object.Destroy(areaIndicator);
    }

    public static void DealHorizontalLineDamageAt(UnitController unit, SkillData skill, Vector3 origin, double finalDamage)
    {
        if (!IsValidUnit(unit) || skill == null)
            return;

        float length = Mathf.Max(0f, skill.lineLength);
        float halfWidth = Mathf.Max(0f, skill.lineWidth) * 0.5f;

        if (length <= 0f || halfWidth <= 0f)
            return;

        if (skill.showLineIndicator)
            SpawnLineIndicator(origin, skill);

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            Vector3 offset = monster.transform.position - origin;
            if (!IsInsideHorizontalLine(offset, length, halfWidth, skill.lineDirection))
                continue;

            DamageSystem.DealDamage(unit, monster, finalDamage);
        }
    }

    private static bool IsInsideHorizontalLine(Vector3 offset, float length, float halfWidth, SkillLineDirection direction)
    {
        if (Mathf.Abs(offset.y) > halfWidth)
            return false;

        switch (direction)
        {
            case SkillLineDirection.Left:
                return offset.x <= 0f && Mathf.Abs(offset.x) <= length;

            case SkillLineDirection.Right:
                return offset.x >= 0f && offset.x <= length;

            default:
                return Mathf.Abs(offset.x) <= length;
        }
    }

    private static void SpawnLineIndicator(Vector3 origin, SkillData skill)
    {
        GameObject indicatorObject = new GameObject("HorizontalLineSkillIndicator");
        indicatorObject.transform.position = GetLineIndicatorCenter(origin, skill);
        indicatorObject.transform.localScale = GetLineIndicatorScale(skill);

        SpriteRenderer spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSquareSprite();
        spriteRenderer.color = skill.lineIndicatorColor;
        spriteRenderer.sortingOrder = 20;

        Object.Destroy(indicatorObject, Mathf.Max(0.01f, skill.lineIndicatorDuration));
    }

    private static Vector3 GetLineIndicatorCenter(Vector3 origin, SkillData skill)
    {
        float halfLength = Mathf.Max(0f, skill.lineLength) * 0.5f;

        switch (skill.lineDirection)
        {
            case SkillLineDirection.Left:
                return origin + new Vector3(-halfLength, 0f, 0f);

            case SkillLineDirection.Right:
                return origin + new Vector3(halfLength, 0f, 0f);

            default:
                return origin;
        }
    }

    private static Vector3 GetLineIndicatorScale(SkillData skill)
    {
        float length = Mathf.Max(0.01f, skill.lineLength);
        float width = Mathf.Max(0.01f, skill.lineWidth);
        float displayLength = skill.lineDirection == SkillLineDirection.Both ? length * 2f : length;
        return new Vector3(displayLength, width, 1f);
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
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);

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
