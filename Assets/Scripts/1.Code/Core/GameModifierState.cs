using System.Collections.Generic;
using UnityEngine;

public static class GameModifierState
{
    private static readonly Dictionary<UnitEnhanceGroup, int> EnhancementLevels = new()
    {
        { UnitEnhanceGroup.LowGradeGroup, 0 },
        { UnitEnhanceGroup.HighGradeGroup, 0 },
        { UnitEnhanceGroup.EvolutionGroup, 0 }
    };

    public static float GlobalAttackSpeedBonus { get; private set; }
    public static float GlobalAttackPowerBonus { get; private set; }
    public static float AngelDemonCooldownReduction { get; private set; }
    public static float MonsterMoveSpeedReduction { get; private set; }
    public static float AngelDemonSkillDamageBonus { get; private set; }
    public static int StageStartBonusGold { get; private set; }
    public static float HigherGradeSummonChanceBonus { get; private set; }
    public static float MergeTwoGradeUpChance { get; private set; }
    public static float UnitExchangeCostReduction { get; private set; }

    public static void ResetBattleState()
    {
        ResetEnhancementLevels();

        GlobalAttackSpeedBonus = 0f;
        GlobalAttackPowerBonus = 0f;
        AngelDemonCooldownReduction = 0f;
        MonsterMoveSpeedReduction = 0f;
        AngelDemonSkillDamageBonus = 0f;
        StageStartBonusGold = 0;
        HigherGradeSummonChanceBonus = 0f;
        MergeTwoGradeUpChance = 0f;
        UnitExchangeCostReduction = 0f;

        RecalculateAllUnitStats();
    }

    public static void ApplyAuctionReward(AuctionRewardOption option)
    {
        if (option == null)
            return;

        switch (option.rewardType)
        {
            case AuctionRewardType.GlobalAttackSpeedUp:
                GlobalAttackSpeedBonus = Mathf.Min(0.5f, GlobalAttackSpeedBonus + 0.05f);
                break;

            case AuctionRewardType.GlobalAttackPowerUp:
                GlobalAttackPowerBonus = Mathf.Min(1f, GlobalAttackPowerBonus + 0.1f);
                break;

            case AuctionRewardType.AngelDemonCooldownReduction:
                AngelDemonCooldownReduction = Mathf.Min(0.4f, AngelDemonCooldownReduction + 0.05f);
                break;

            case AuctionRewardType.MonsterMoveSpeedReduction:
                MonsterMoveSpeedReduction = Mathf.Min(0.3f, MonsterMoveSpeedReduction + 0.05f);
                break;

            case AuctionRewardType.AngelDemonSkillDamageUp:
                AngelDemonSkillDamageBonus = Mathf.Min(1f, AngelDemonSkillDamageBonus + 0.05f);
                break;

            case AuctionRewardType.StageStartBonusGold:
                StageStartBonusGold += 10;
                break;

            case AuctionRewardType.HigherGradeSummonChanceUp:
                HigherGradeSummonChanceBonus = Mathf.Min(0.2f, HigherGradeSummonChanceBonus + 0.02f);
                break;

            case AuctionRewardType.MergeTwoGradeUpChance:
                MergeTwoGradeUpChance = Mathf.Min(0.15f, MergeTwoGradeUpChance + 0.03f);
                break;

            case AuctionRewardType.UnitExchangeCostReduction:
                UnitExchangeCostReduction = Mathf.Min(0.3f, UnitExchangeCostReduction + 0.03f);
                break;
        }
    }

    public static int GetEnhancementLevel(UnitEnhanceGroup group)
    {
        return EnhancementLevels.TryGetValue(group, out int level) ? level : 0;
    }

    public static int GetNextEnhancementCost(UnitEnhanceGroup group)
    {
        int nextLevel = Mathf.Min(GameBalanceConfig.MaxEnhancementLevel, GetEnhancementLevel(group) + 1);

        if (!GameBalanceConfig.TryGetEnhancementData(group, nextLevel, out EnhancementLevelData data))
            return 0;

        return data.cost;
    }

    public static bool TryEnhance(UnitEnhanceGroup group, GoldManager goldManager)
    {
        BattleMagicStoneManager magicStoneManager = BattleMagicStoneManager.Instance;
        if (magicStoneManager == null)
            magicStoneManager = Object.FindAnyObjectByType<BattleMagicStoneManager>();

        return magicStoneManager != null && magicStoneManager.TryUpgradeGradeGroup(group);
    }

    public static bool SetEnhancementLevel(UnitEnhanceGroup group, int level)
    {
        if (!EnhancementLevels.ContainsKey(group))
            return false;

        EnhancementLevels[group] = Mathf.Clamp(level, 0, GameBalanceConfig.MaxEnhancementLevel);
        RecalculateAllUnitStats();
        return true;
    }

    public static UnitEnhanceGroup GetEnhancementGroup(UnitGrade grade)
    {
        return GameBalanceConfig.GetEnhanceGroup(grade);
    }

    public static int GetReducedUnitExchangeCost(int baseCost)
    {
        if (baseCost < 0)
            return GameBalanceConfig.UnitExchangeUnavailableCost;

        return Mathf.Max(1, Mathf.RoundToInt(baseCost * (1f - UnitExchangeCostReduction)));
    }

    public static float GetEnhancementAttackPowerMultiplier(UnitGrade grade)
    {
        UnitEnhanceGroup group = GameBalanceConfig.GetEnhanceGroup(grade);
        int level = GetEnhancementLevel(group);

        return GameBalanceConfig.TryGetEnhancementData(group, level, out EnhancementLevelData data)
            ? data.attackPowerMultiplier
            : 1f;
    }

    public static float GetEnhancementAttackSpeedMultiplier(UnitGrade grade)
    {
        UnitEnhanceGroup group = GameBalanceConfig.GetEnhanceGroup(grade);
        int level = GetEnhancementLevel(group);

        return GameBalanceConfig.TryGetEnhancementData(group, level, out EnhancementLevelData data)
            ? data.attackSpeedMultiplier
            : 1f;
    }

    public static bool IsEvolutionGrade(UnitData unitData)
    {
        return unitData != null
            && (unitData.grade == UnitGrade.ArchAngel || unitData.grade == UnitGrade.GreatDemon);
    }

    private static void RecalculateAllUnitStats()
    {
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        foreach (UnitController unit in units)
        {
            if (unit != null)
                unit.RecalculateStats();
        }
    }

    private static void ResetEnhancementLevels()
    {
        EnhancementLevels[UnitEnhanceGroup.LowGradeGroup] = 0;
        EnhancementLevels[UnitEnhanceGroup.HighGradeGroup] = 0;
        EnhancementLevels[UnitEnhanceGroup.EvolutionGroup] = 0;
    }
}
