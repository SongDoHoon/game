using System.Collections.Generic;
using UnityEngine;

public static class GameBalanceConfig
{
    public const int MaxEnhancementLevel = 30;
    public const int StageClearGold = 10;
    public const int UnitExchangeUnavailableCost = -1;

    private static readonly int[] BossClearGoldByStage = { 50, 100, 200, 250, 300, 350, 400, 450, 500, 0 };

    private static readonly Dictionary<UnitEnhanceGroup, EnhancementLevelData[]> EnhancementTables = new()
    {
        {
            UnitEnhanceGroup.LowGradeGroup,
            new[]
            {
                new EnhancementLevelData(1f, 1f, 0),
                new EnhancementLevelData(1.05f, 1.01f, 10),
                new EnhancementLevelData(1.10f, 1.01f, 15),
                new EnhancementLevelData(1.15f, 1.02f, 20),
                new EnhancementLevelData(1.20f, 1.02f, 25),
                new EnhancementLevelData(1.25f, 1.03f, 30),
                new EnhancementLevelData(1.31f, 1.03f, 39),
                new EnhancementLevelData(1.37f, 1.04f, 48),
                new EnhancementLevelData(1.43f, 1.04f, 57),
                new EnhancementLevelData(1.49f, 1.05f, 66),
                new EnhancementLevelData(1.55f, 1.05f, 75),
                new EnhancementLevelData(1.62f, 1.06f, 89),
                new EnhancementLevelData(1.69f, 1.06f, 103),
                new EnhancementLevelData(1.76f, 1.07f, 117),
                new EnhancementLevelData(1.83f, 1.07f, 131),
                new EnhancementLevelData(1.90f, 1.08f, 145),
                new EnhancementLevelData(1.98f, 1.08f, 164),
                new EnhancementLevelData(2.06f, 1.09f, 183),
                new EnhancementLevelData(2.14f, 1.09f, 202),
                new EnhancementLevelData(2.22f, 1.10f, 221),
                new EnhancementLevelData(2.30f, 1.10f, 240),
                new EnhancementLevelData(2.39f, 1.11f, 265),
                new EnhancementLevelData(2.48f, 1.11f, 290),
                new EnhancementLevelData(2.57f, 1.12f, 315),
                new EnhancementLevelData(2.66f, 1.12f, 340),
                new EnhancementLevelData(2.75f, 1.13f, 365),
                new EnhancementLevelData(2.84f, 1.13f, 396),
                new EnhancementLevelData(2.93f, 1.14f, 427),
                new EnhancementLevelData(3.02f, 1.14f, 458),
                new EnhancementLevelData(3.11f, 1.15f, 489),
                new EnhancementLevelData(3.20f, 1.15f, 520)
            }
        },
        {
            UnitEnhanceGroup.HighGradeGroup,
            new[]
            {
                new EnhancementLevelData(1f, 1f, 0),
                new EnhancementLevelData(1.08f, 1.01f, 18),
                new EnhancementLevelData(1.16f, 1.02f, 27),
                new EnhancementLevelData(1.24f, 1.02f, 36),
                new EnhancementLevelData(1.32f, 1.03f, 45),
                new EnhancementLevelData(1.40f, 1.04f, 54),
                new EnhancementLevelData(1.50f, 1.05f, 70),
                new EnhancementLevelData(1.59f, 1.06f, 86),
                new EnhancementLevelData(1.69f, 1.06f, 103),
                new EnhancementLevelData(1.78f, 1.07f, 119),
                new EnhancementLevelData(1.88f, 1.08f, 135),
                new EnhancementLevelData(1.99f, 1.09f, 160),
                new EnhancementLevelData(2.11f, 1.10f, 185),
                new EnhancementLevelData(2.22f, 1.10f, 210),
                new EnhancementLevelData(2.34f, 1.11f, 235),
                new EnhancementLevelData(2.45f, 1.12f, 260),
                new EnhancementLevelData(2.59f, 1.13f, 294),
                new EnhancementLevelData(2.73f, 1.14f, 328),
                new EnhancementLevelData(2.87f, 1.14f, 362),
                new EnhancementLevelData(3.01f, 1.15f, 396),
                new EnhancementLevelData(3.15f, 1.16f, 430),
                new EnhancementLevelData(3.33f, 1.17f, 476),
                new EnhancementLevelData(3.51f, 1.18f, 522),
                new EnhancementLevelData(3.69f, 1.18f, 568),
                new EnhancementLevelData(3.87f, 1.19f, 614),
                new EnhancementLevelData(4.05f, 1.20f, 660),
                new EnhancementLevelData(4.30f, 1.21f, 716),
                new EnhancementLevelData(4.55f, 1.22f, 772),
                new EnhancementLevelData(4.80f, 1.22f, 828),
                new EnhancementLevelData(5.05f, 1.23f, 884),
                new EnhancementLevelData(5.30f, 1.24f, 940)
            }
        },
        {
            UnitEnhanceGroup.EvolutionGroup,
            new[]
            {
                new EnhancementLevelData(1f, 1f, 0),
                new EnhancementLevelData(1.12f, 1.01f, 35),
                new EnhancementLevelData(1.24f, 1.02f, 52),
                new EnhancementLevelData(1.36f, 1.03f, 70),
                new EnhancementLevelData(1.48f, 1.04f, 88),
                new EnhancementLevelData(1.60f, 1.05f, 105),
                new EnhancementLevelData(1.75f, 1.06f, 136),
                new EnhancementLevelData(1.90f, 1.07f, 167),
                new EnhancementLevelData(2.05f, 1.08f, 198),
                new EnhancementLevelData(2.20f, 1.09f, 229),
                new EnhancementLevelData(2.35f, 1.10f, 260),
                new EnhancementLevelData(2.54f, 1.11f, 308),
                new EnhancementLevelData(2.73f, 1.12f, 356),
                new EnhancementLevelData(2.92f, 1.13f, 404),
                new EnhancementLevelData(3.11f, 1.14f, 452),
                new EnhancementLevelData(3.30f, 1.15f, 500),
                new EnhancementLevelData(3.55f, 1.16f, 566),
                new EnhancementLevelData(3.80f, 1.17f, 632),
                new EnhancementLevelData(4.05f, 1.18f, 698),
                new EnhancementLevelData(4.30f, 1.19f, 764),
                new EnhancementLevelData(4.55f, 1.20f, 830),
                new EnhancementLevelData(4.89f, 1.21f, 914),
                new EnhancementLevelData(5.23f, 1.22f, 998),
                new EnhancementLevelData(5.57f, 1.23f, 1082),
                new EnhancementLevelData(5.91f, 1.24f, 1166),
                new EnhancementLevelData(6.25f, 1.25f, 1250),
                new EnhancementLevelData(6.80f, 1.26f, 1360),
                new EnhancementLevelData(7.35f, 1.27f, 1470),
                new EnhancementLevelData(7.90f, 1.28f, 1580),
                new EnhancementLevelData(8.45f, 1.29f, 1690),
                new EnhancementLevelData(9.00f, 1.30f, 1800)
            }
        }
    };

    public static int GetNormalKillGold()
    {
        return 1;
    }

    public static int GetStageClearGold()
    {
        return StageClearGold;
    }

    public static int GetBossClearGold(int stage)
    {
        if (!MonsterBalanceCalculator.IsBossWave(stage))
            return 0;

        int index = GetBossStageIndex(stage);
        return index >= 0 ? BossClearGoldByStage[index] : 0;
    }

    public static bool TryGetEnhancementData(UnitEnhanceGroup group, int level, out EnhancementLevelData data)
    {
        data = default;

        if (!EnhancementTables.TryGetValue(group, out EnhancementLevelData[] table))
            return false;

        int clampedLevel = Mathf.Clamp(level, 0, table.Length - 1);
        data = table[clampedLevel];
        return true;
    }

    public static UnitEnhanceGroup GetEnhanceGroup(UnitGrade grade)
    {
        switch (grade)
        {
            case UnitGrade.Normal:
            case UnitGrade.Rare:
                return UnitEnhanceGroup.LowGradeGroup;

            case UnitGrade.Epic:
            case UnitGrade.Verure:
                return UnitEnhanceGroup.HighGradeGroup;

            case UnitGrade.ArchAngel:
            case UnitGrade.GreatDemon:
                return UnitEnhanceGroup.EvolutionGroup;

            default:
                return UnitEnhanceGroup.LowGradeGroup;
        }
    }

    public static bool CanExchangeUnitGrade(UnitGrade grade)
    {
        return grade == UnitGrade.Normal
            || grade == UnitGrade.Rare
            || grade == UnitGrade.Epic
            || grade == UnitGrade.Verure;
    }

    public static int GetUnitExchangeBaseCost(UnitGrade grade)
    {
        switch (grade)
        {
            case UnitGrade.Normal:
                return 20;

            case UnitGrade.Rare:
                return 45;

            case UnitGrade.Epic:
                return 110;

            case UnitGrade.Verure:
                return 260;

            default:
                return UnitExchangeUnavailableCost;
        }
    }

    private static int GetBossStageIndex(int stage)
    {
        if (stage < 10 || stage > 100 || stage % 10 != 0)
            return -1;

        return (stage / 10) - 1;
    }


}

public struct EnhancementLevelData
{
    public float attackPowerMultiplier;
    public float attackSpeedMultiplier;
    public int cost;

    public EnhancementLevelData(float attackPowerMultiplier, float attackSpeedMultiplier, int cost)
    {
        this.attackPowerMultiplier = attackPowerMultiplier;
        this.attackSpeedMultiplier = attackSpeedMultiplier;
        this.cost = cost;
    }
}
