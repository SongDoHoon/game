using UnityEngine;

public static class UnitStatCalculator
{
    public static UnitStatModifierResult Calculate(
        UnitData unitData,
        UnitGrowthEntry unitGrowth,
        PlayerPassiveGrowthData playerPassiveGrowth,
        double commonAttackBonus,
        float commonAttackSpeedBonus)
    {
        if (unitData == null)
            return default;

        return Calculate(
            unitData,
            unitGrowth,
            playerPassiveGrowth,
            commonAttackBonus,
            commonAttackSpeedBonus,
            0.0,
            0f);
    }

    public static UnitStatModifierResult Calculate(
        UnitData unitData,
        UnitGrowthEntry unitGrowth,
        PlayerPassiveGrowthData playerPassiveGrowth,
        double commonAttackBonus,
        float commonAttackSpeedBonus,
        double runtimeAttackBonus,
        float runtimeAttackSpeedBonus)
    {
        if (unitData == null)
            return default;

        double baseAttack = unitData.attackPower;
        float baseAttackInterval = 1f / Mathf.Max(0.01f, unitData.attackSpeed);

        int shardLevel = unitGrowth != null ? unitGrowth.unitShardLevel : 0;
        int magicStoneLevel = unitGrowth != null ? unitGrowth.magicStoneLevel : 0;
        int equipmentLevel = unitGrowth != null ? unitGrowth.equipmentLevel : 0;
        int relicLevel = unitGrowth != null ? unitGrowth.relicLevel : 0;

        UnitShardUpgradeData shardData = UnitGrowthBalanceConfig.GetUnitShardData(shardLevel);
        MagicStoneUpgradeData magicStoneData = UnitGrowthBalanceConfig.GetMagicStoneData(magicStoneLevel);
        EquipmentUpgradeData equipmentData = UnitGrowthBalanceConfig.GetEquipmentData(equipmentLevel);
        RelicUpgradeData relicData = UnitGrowthBalanceConfig.GetRelicData(relicLevel);

        double inGameGradeMultiplier = GameModifierState.GetEnhancementAttackPowerMultiplier(unitData.grade);
        float inGameGradeAttackSpeedMultiplier = GameModifierState.GetEnhancementAttackSpeedMultiplier(unitData.grade);

        double playerPassiveAttackBonus = GetTotalPlayerPassiveAttackBonus(playerPassiveGrowth);
        float playerPassiveAttackSpeedBonus = GetTotalPlayerPassiveAttackSpeedBonus(playerPassiveGrowth);

        double additiveAttackBonus = shardData.attackBonus
            + playerPassiveAttackBonus
            + commonAttackBonus
            + runtimeAttackBonus;

        float additiveAttackSpeedBonus = shardData.attackSpeedBonus
            + playerPassiveAttackSpeedBonus
            + commonAttackSpeedBonus
            + runtimeAttackSpeedBonus;

        double finalAttack = baseAttack
            * inGameGradeMultiplier
            * magicStoneData.attackMultiplier
            * equipmentData.attackMultiplier
            * relicData.attackMultiplier
            * (1.0 + additiveAttackBonus);

        float attackSpeedMultiplier = inGameGradeAttackSpeedMultiplier
            * magicStoneData.attackSpeedMultiplier;

        float finalAttackInterval = baseAttackInterval
            / Mathf.Max(0.01f, inGameGradeAttackSpeedMultiplier)
            / Mathf.Max(0.01f, magicStoneData.attackSpeedMultiplier)
            / 1f
            / 1f
            / Mathf.Max(0.01f, 1f + additiveAttackSpeedBonus);

        finalAttackInterval = Mathf.Max(UnitGrowthBalanceConfig.MinimumAttackInterval, finalAttackInterval);

        return new UnitStatModifierResult
        {
            finalAttack = finalAttack,
            finalAttackInterval = finalAttackInterval,
            baseAttack = baseAttack,
            baseAttackInterval = baseAttackInterval,
            inGameGradeMultiplier = inGameGradeMultiplier,
            magicStoneMultiplier = magicStoneData.attackMultiplier,
            equipmentMultiplier = equipmentData.attackMultiplier,
            relicMultiplier = relicData.attackMultiplier,
            additiveAttackBonus = additiveAttackBonus,
            attackSpeedMultiplier = attackSpeedMultiplier,
            additiveAttackSpeedBonus = additiveAttackSpeedBonus
        };
    }

    public static float GetSkillDamageMultiplier(UnitGrowthEntry unitGrowth)
    {
        int skillLevel = unitGrowth != null ? unitGrowth.skillLevel : 0;
        WishStoneSkillUpgradeData wishStoneData = UnitGrowthBalanceConfig.GetWishStoneSkillData(skillLevel);
        EquipmentUpgradeData equipmentData = UnitGrowthBalanceConfig.GetEquipmentData(unitGrowth != null ? unitGrowth.equipmentLevel : 0);

        return 1f + wishStoneData.skillDamageBonus + equipmentData.skillDamageBonus;
    }

    public static float GetSkillCooldownReduction(UnitGrowthEntry unitGrowth)
    {
        int skillLevel = unitGrowth != null ? unitGrowth.skillLevel : 0;
        WishStoneSkillUpgradeData wishStoneData = UnitGrowthBalanceConfig.GetWishStoneSkillData(skillLevel);

        return Mathf.Clamp01(wishStoneData.cooldownReduction);
    }

    public static float GetRelicSpecialAbilityBonus(UnitGrowthEntry unitGrowth)
    {
        int relicLevel = unitGrowth != null ? unitGrowth.relicLevel : 0;
        return UnitGrowthBalanceConfig.GetRelicData(relicLevel).specialAbilityBonus;
    }

    public static bool IsRelicSpecialAbilityUnlocked(UnitGrowthEntry unitGrowth)
    {
        int relicLevel = unitGrowth != null ? unitGrowth.relicLevel : 0;
        return UnitGrowthBalanceConfig.GetRelicData(relicLevel).specialAbilityUnlocked;
    }

    private static double GetTotalPlayerPassiveAttackBonus(PlayerPassiveGrowthData playerPassiveGrowth)
    {
        if (playerPassiveGrowth == null || playerPassiveGrowth.passives == null)
            return 0.0;

        double total = 0.0;
        foreach (PlayerPassiveGrowthEntry passive in playerPassiveGrowth.passives)
        {
            if (passive == null)
                continue;

            total += UnitGrowthBalanceConfig.GetPlayerPassiveData(passive.level).attackBonus;
        }

        return total;
    }

    private static float GetTotalPlayerPassiveAttackSpeedBonus(PlayerPassiveGrowthData playerPassiveGrowth)
    {
        if (playerPassiveGrowth == null || playerPassiveGrowth.passives == null)
            return 0f;

        float total = 0f;
        foreach (PlayerPassiveGrowthEntry passive in playerPassiveGrowth.passives)
        {
            if (passive == null)
                continue;

            total += UnitGrowthBalanceConfig.GetPlayerPassiveData(passive.level).attackSpeedBonus;
        }

        return total;
    }
}
