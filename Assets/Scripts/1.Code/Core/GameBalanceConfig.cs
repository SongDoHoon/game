using System.Collections.Generic;
using UnityEngine;

public static class GameBalanceConfig
{
    private static readonly int[] BossClearGoldByStage = { 50, 100, 200, 250, 300, 350, 400, 450, 500, 0 };
    private static readonly int[] AuctionBasePriceByStage = { 20, 40, 75, 95, 120, 145, 170, 200, 240, 0 };

    private static readonly AuctionRewardType[] NormalAuctionRewards =
    {
        AuctionRewardType.GlobalAttackSpeedUp,
        AuctionRewardType.GlobalAttackPowerUp,
        AuctionRewardType.AngelDemonCooldownReduction,
        AuctionRewardType.MonsterMoveSpeedReduction,
        AuctionRewardType.AngelDemonSkillDamageUp,
        AuctionRewardType.StageStartBonusGold,
        AuctionRewardType.HigherGradeSummonChanceUp,
        AuctionRewardType.MergeTwoGradeUpChance,
        AuctionRewardType.UnitExchangeCostReduction
    };

    private static readonly EvolutionItemType[] EvolutionAuctionRewards =
    {
        EvolutionItemType.MichaelItem,
        EvolutionItemType.GabrielItem,
        EvolutionItemType.RaphaelItem,
        EvolutionItemType.UrielItem,
        EvolutionItemType.RaguelItem,
        EvolutionItemType.SarielItem,
        EvolutionItemType.DemonItem1,
        EvolutionItemType.DemonItem2,
        EvolutionItemType.DemonItem3,
        EvolutionItemType.DemonItem4,
        EvolutionItemType.DemonItem5,
        EvolutionItemType.DemonItem6,
        EvolutionItemType.DemonItem7
    };

    private static readonly Dictionary<AuctionRewardType, float> AuctionPriceMultipliers = new()
    {
        { AuctionRewardType.GlobalAttackSpeedUp, 1f },
        { AuctionRewardType.GlobalAttackPowerUp, 1.15f },
        { AuctionRewardType.AngelDemonCooldownReduction, 1.05f },
        { AuctionRewardType.MonsterMoveSpeedReduction, 0.95f },
        { AuctionRewardType.AngelDemonSkillDamageUp, 1.05f },
        { AuctionRewardType.StageStartBonusGold, 1.2f },
        { AuctionRewardType.HigherGradeSummonChanceUp, 1.25f },
        { AuctionRewardType.MergeTwoGradeUpChance, 1.35f },
        { AuctionRewardType.UnitExchangeCostReduction, 0.85f },
        { AuctionRewardType.EvolutionItem, 1.5f }
    };

    private static readonly Dictionary<UnitEnhanceGroup, EnhancementLevelData[]> EnhancementTables = new()
    {
        {
            UnitEnhanceGroup.LowGradeGroup,
            new[]
            {
                new EnhancementLevelData(1f, 1f, 0),
                new EnhancementLevelData(1.12f, 1.01f, 10),
                new EnhancementLevelData(1.25f, 1.02f, 18),
                new EnhancementLevelData(1.4f, 1.03f, 30),
                new EnhancementLevelData(1.57f, 1.04f, 45),
                new EnhancementLevelData(1.76f, 1.05f, 65),
                new EnhancementLevelData(1.97f, 1.06f, 90),
                new EnhancementLevelData(2.2f, 1.07f, 120),
                new EnhancementLevelData(2.46f, 1.08f, 160),
                new EnhancementLevelData(2.75f, 1.09f, 210),
                new EnhancementLevelData(3.08f, 1.1f, 280)
            }
        },
        {
            UnitEnhanceGroup.HighGradeGroup,
            new[]
            {
                new EnhancementLevelData(1f, 1f, 0),
                new EnhancementLevelData(1.18f, 1.01f, 20),
                new EnhancementLevelData(1.39f, 1.02f, 35),
                new EnhancementLevelData(1.64f, 1.03f, 55),
                new EnhancementLevelData(1.94f, 1.04f, 80),
                new EnhancementLevelData(2.29f, 1.05f, 115),
                new EnhancementLevelData(2.7f, 1.07f, 160),
                new EnhancementLevelData(3.19f, 1.09f, 220),
                new EnhancementLevelData(3.76f, 1.11f, 300),
                new EnhancementLevelData(4.44f, 1.13f, 400),
                new EnhancementLevelData(5.24f, 1.15f, 520)
            }
        },
        {
            UnitEnhanceGroup.EvolutionGroup,
            new[]
            {
                new EnhancementLevelData(1f, 1f, 0),
                new EnhancementLevelData(1.25f, 1.01f, 40),
                new EnhancementLevelData(1.56f, 1.02f, 70),
                new EnhancementLevelData(1.95f, 1.03f, 110),
                new EnhancementLevelData(2.44f, 1.04f, 165),
                new EnhancementLevelData(3.05f, 1.06f, 240),
                new EnhancementLevelData(3.81f, 1.08f, 340),
                new EnhancementLevelData(4.76f, 1.1f, 470),
                new EnhancementLevelData(5.95f, 1.12f, 640),
                new EnhancementLevelData(7.44f, 1.15f, 850),
                new EnhancementLevelData(9.31f, 1.18f, 1100)
            }
        }
    };

    public static int GetNormalKillGold()
    {
        return 1;
    }

    public static int GetBossClearGold(int stage)
    {
        if (!MonsterBalanceCalculator.IsBossWave(stage))
            return 0;

        int index = GetBossStageIndex(stage);
        return index >= 0 ? BossClearGoldByStage[index] : 0;
    }

    public static bool HasAuctionAtStage(int stage)
    {
        return stage >= 10 && stage <= 90 && stage % 10 == 0;
    }

    public static int GetAuctionBasePrice(int stage)
    {
        int index = GetBossStageIndex(stage);
        return index >= 0 ? AuctionBasePriceByStage[index] : 0;
    }

    public static float GetAuctionPriceMultiplier(AuctionRewardType rewardType)
    {
        return AuctionPriceMultipliers.TryGetValue(rewardType, out float multiplier) ? multiplier : 1f;
    }

    public static int GetAuctionStartPrice(int stage, AuctionRewardType rewardType)
    {
        return Mathf.RoundToInt(GetAuctionBasePrice(stage) * GetAuctionPriceMultiplier(rewardType));
    }

    public static AuctionRewardOption[] CreateAuctionOptions(int stage)
    {
        if (!HasAuctionAtStage(stage))
            return new AuctionRewardOption[0];

        List<AuctionRewardOption> options = new();
        int evolutionCount = GetEvolutionOptionCount(stage);
        AddEvolutionOptions(options, stage, evolutionCount);
        AddNormalOptions(options, stage, 4 - options.Count);
        Shuffle(options);

        return options.ToArray();
    }

    public static AuctionAIPersonality RollAIPersonality(int stage)
    {
        float roll = Random.value;

        if (stage <= 20)
            return roll < 0.4f ? AuctionAIPersonality.Passive : roll < 0.85f ? AuctionAIPersonality.Normal : AuctionAIPersonality.Aggressive;

        if (stage <= 60)
            return roll < 0.25f ? AuctionAIPersonality.Passive : roll < 0.75f ? AuctionAIPersonality.Normal : AuctionAIPersonality.Aggressive;

        return roll < 0.15f ? AuctionAIPersonality.Passive : roll < 0.6f ? AuctionAIPersonality.Normal : AuctionAIPersonality.Aggressive;
    }

    public static float GetAIBudgetMultiplier(AuctionAIPersonality personality)
    {
        return personality switch
        {
            AuctionAIPersonality.Passive => 1.25f,
            AuctionAIPersonality.Normal => 1.6f,
            AuctionAIPersonality.Aggressive => 2.1f,
            _ => 1.25f
        };
    }

    public static float GetAIRebidChance(float burdenRate)
    {
        if (burdenRate > 1f) return 0f;
        if (burdenRate <= 0.6f) return 0.9f;
        if (burdenRate <= 0.75f) return 0.7f;
        if (burdenRate <= 0.9f) return 0.45f;
        return 0.2f;
    }

    public static int GetMinBidIncrease(int currentPrice)
    {
        if (currentPrice < 20) return 1;
        if (currentPrice < 50) return 3;
        if (currentPrice < 100) return 5;
        if (currentPrice < 300) return 10;
        if (currentPrice < 800) return 25;
        return 50;
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

    private static int GetBossStageIndex(int stage)
    {
        if (stage < 10 || stage > 100 || stage % 10 != 0)
            return -1;

        return (stage / 10) - 1;
    }

    private static int GetEvolutionOptionCount(int stage)
    {
        if (stage < 30)
            return 0;

        if (stage <= 60)
            return 2;

        return Random.Range(2, 4);
    }

    private static void AddEvolutionOptions(List<AuctionRewardOption> options, int stage, int count)
    {
        List<EvolutionItemType> pool = new(EvolutionAuctionRewards);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            EvolutionItemType item = pool[index];
            pool.RemoveAt(index);

            options.Add(AuctionRewardOption.CreateEvolutionItem(item, stage));
        }
    }

    private static void AddNormalOptions(List<AuctionRewardOption> options, int stage, int count)
    {
        List<AuctionRewardType> pool = new(NormalAuctionRewards);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            AuctionRewardType rewardType = pool[index];
            pool.RemoveAt(index);

            options.Add(AuctionRewardOption.CreateReward(rewardType, stage));
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int swapIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
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
