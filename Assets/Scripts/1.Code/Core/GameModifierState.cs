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

    public static void ResetBattleState()
    {
        ResetEnhancementLevels();

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
