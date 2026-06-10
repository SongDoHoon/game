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
    public static float ContractAttackPowerBonus { get; private set; }
    public static float ContractAttackSpeedBonus { get; private set; }
    public static float ContractMonsterMoveSpeedReduction { get; private set; }

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
        ContractAttackPowerBonus = 0f;
        ContractAttackSpeedBonus = 0f;
        ContractMonsterMoveSpeedReduction = 0f;

        RecalculateAllUnitStats();
    }

    public static void ApplyAuctionReward(AuctionRewardOption option)
    {
        Debug.Log("[Auction] 경매 아이템 효과 modifier 제거 완료: ApplyAuctionReward는 더 이상 효과를 적용하지 않습니다.");
    }

    public static void SetContractModifiers(float attackPowerBonus, float attackSpeedBonus, float monsterMoveSpeedReduction)
    {
        ContractAttackPowerBonus = attackPowerBonus;
        ContractAttackSpeedBonus = attackSpeedBonus;
        ContractMonsterMoveSpeedReduction = Mathf.Clamp01(monsterMoveSpeedReduction);
        RecalculateAllUnitStats();
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

        return baseCost;
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
