using UnityEngine;

public static class UnitGrowthBalanceConfig
{
    public const int MaxUnitShardLevel = 11;
    public const int MaxMagicStoneLevel = 10;
    public const int MaxEquipmentLevel = 10;
    public const int MaxRelicLevel = 6;
    public const int MaxPlayerPassiveLevel = 10;
    public const int MaxWishStoneSkillLevel = 10;

    public const float MinimumAttackInterval = 0.35f;

    private static readonly UnitShardUpgradeData[] UnitShardTable =
    {
        new(0, 0, 0f, 0f),
        new(0, 0, 0f, 0f),
        new(10, 300, 0.03f, 0.01f),
        new(20, 600, 0.06f, 0.02f),
        new(35, 1000, 0.09f, 0.03f),
        new(55, 1600, 0.12f, 0.04f),
        new(80, 2400, 0.15f, 0.05f),
        new(110, 3500, 0.19f, 0.06f),
        new(150, 5000, 0.23f, 0.07f),
        new(200, 7000, 0.28f, 0.08f),
        new(260, 9500, 0.34f, 0.09f),
        new(330, 13000, 0.40f, 0.10f)
    };

    private static readonly MagicStoneUpgradeData[] MagicStoneTable =
    {
        new(0, 0, 1.00, 1.00f),
        new(3, 500, 1.06, 1.01f),
        new(5, 1000, 1.12, 1.02f),
        new(8, 1800, 1.19, 1.03f),
        new(12, 3000, 1.27, 1.04f),
        new(17, 4500, 1.36, 1.05f),
        new(23, 6500, 1.46, 1.06f),
        new(30, 9000, 1.57, 1.07f),
        new(40, 12000, 1.70, 1.08f),
        new(55, 16000, 1.85, 1.09f),
        new(75, 22000, 2.00, 1.10f)
    };

    private static readonly EquipmentUpgradeData[] EquipmentTable =
    {
        new(0, 1.00, 0f, 0f, false),
        new(1000, 1.05, 0f, 0f, false),
        new(2000, 1.10, 0f, 0f, false),
        new(3500, 1.16, 0f, 0f, false),
        new(5500, 1.23, 0f, 0f, false),
        new(8000, 1.31, 0.03f, 0f, false),
        new(12000, 1.40, 0f, 0f, false),
        new(17000, 1.50, 0f, 0f, false),
        new(23000, 1.62, 0f, 0.05f, false),
        new(31000, 1.75, 0f, 0f, false),
        new(42000, 1.90, 0f, 0f, true)
    };

    private static readonly RelicUpgradeData[] RelicTable =
    {
        new(0, 0, 1.00, 0f, false),
        new(1, 3000, 1.05, 0f, true),
        new(1, 5000, 1.10, 0.10f, true),
        new(2, 9000, 1.16, 0.20f, true),
        new(3, 15000, 1.23, 0.35f, true),
        new(5, 25000, 1.31, 0.50f, true),
        new(8, 40000, 1.40, 0.75f, true)
    };

    private static readonly PlayerPassiveUpgradeData[] PlayerPassiveTable =
    {
        new(0, 0, 0f, 0f),
        new(1, 2000, 0.02f, 0.005f),
        new(1, 4000, 0.04f, 0.010f),
        new(2, 7000, 0.06f, 0.015f),
        new(2, 11000, 0.08f, 0.020f),
        new(3, 16000, 0.10f, 0.025f),
        new(4, 23000, 0.13f, 0.030f),
        new(5, 32000, 0.16f, 0.035f),
        new(7, 45000, 0.19f, 0.040f),
        new(9, 60000, 0.22f, 0.045f),
        new(12, 80000, 0.25f, 0.050f)
    };

    private static readonly WishStoneSkillUpgradeData[] WishStoneSkillTable =
    {
        new(0, 0, 0f, 0f),
        new(1, 1500, 0.08f, 0f),
        new(1, 3000, 0.16f, 0f),
        new(2, 5000, 0.25f, 0f),
        new(2, 8000, 0.35f, 0f),
        new(3, 12000, 0.50f, 0.03f),
        new(4, 18000, 0.65f, 0.03f),
        new(5, 26000, 0.85f, 0.05f),
        new(7, 36000, 1.10f, 0.05f),
        new(9, 50000, 1.40f, 0.07f),
        new(12, 70000, 1.80f, 0.10f)
    };

    public static UnitShardUpgradeData GetUnitShardData(int level)
    {
        return UnitShardTable[Mathf.Clamp(level, 1, MaxUnitShardLevel)];
    }

    public static MagicStoneUpgradeData GetMagicStoneData(int level)
    {
        return MagicStoneTable[Mathf.Clamp(level, 0, MaxMagicStoneLevel)];
    }

    public static EquipmentUpgradeData GetEquipmentData(int level)
    {
        return EquipmentTable[Mathf.Clamp(level, 0, MaxEquipmentLevel)];
    }

    public static RelicUpgradeData GetRelicData(int level)
    {
        return RelicTable[Mathf.Clamp(level, 0, MaxRelicLevel)];
    }

    public static PlayerPassiveUpgradeData GetPlayerPassiveData(int level)
    {
        return PlayerPassiveTable[Mathf.Clamp(level, 0, MaxPlayerPassiveLevel)];
    }

    public static WishStoneSkillUpgradeData GetWishStoneSkillData(int level)
    {
        return WishStoneSkillTable[Mathf.Clamp(level, 0, MaxWishStoneSkillLevel)];
    }
}

public struct UnitShardUpgradeData
{
    public readonly int shardCost;
    public readonly int goldCost;
    public readonly float attackBonus;
    public readonly float attackSpeedBonus;

    public UnitShardUpgradeData(int shardCost, int goldCost, float attackBonus, float attackSpeedBonus)
    {
        this.shardCost = shardCost;
        this.goldCost = goldCost;
        this.attackBonus = attackBonus;
        this.attackSpeedBonus = attackSpeedBonus;
    }
}

public struct MagicStoneUpgradeData
{
    public readonly int stoneCost;
    public readonly int goldCost;
    public readonly double attackMultiplier;
    public readonly float attackSpeedMultiplier;

    public MagicStoneUpgradeData(int stoneCost, int goldCost, double attackMultiplier, float attackSpeedMultiplier)
    {
        this.stoneCost = stoneCost;
        this.goldCost = goldCost;
        this.attackMultiplier = attackMultiplier;
        this.attackSpeedMultiplier = attackSpeedMultiplier;
    }
}

public struct EquipmentUpgradeData
{
    public readonly int goldCost;
    public readonly double attackMultiplier;
    public readonly float skillDamageBonus;
    public readonly float bossDamageBonus;
    public readonly bool hasExclusiveEffectUpgrade;

    public EquipmentUpgradeData(int goldCost, double attackMultiplier, float skillDamageBonus, float bossDamageBonus, bool hasExclusiveEffectUpgrade)
    {
        this.goldCost = goldCost;
        this.attackMultiplier = attackMultiplier;
        this.skillDamageBonus = skillDamageBonus;
        this.bossDamageBonus = bossDamageBonus;
        this.hasExclusiveEffectUpgrade = hasExclusiveEffectUpgrade;
    }
}

public struct RelicUpgradeData
{
    public readonly int relicCost;
    public readonly int goldCost;
    public readonly double attackMultiplier;
    public readonly float specialAbilityBonus;
    public readonly bool specialAbilityUnlocked;

    public RelicUpgradeData(int relicCost, int goldCost, double attackMultiplier, float specialAbilityBonus, bool specialAbilityUnlocked)
    {
        this.relicCost = relicCost;
        this.goldCost = goldCost;
        this.attackMultiplier = attackMultiplier;
        this.specialAbilityBonus = specialAbilityBonus;
        this.specialAbilityUnlocked = specialAbilityUnlocked;
    }
}

public struct PlayerPassiveUpgradeData
{
    public readonly int stoneCost;
    public readonly int goldCost;
    public readonly float attackBonus;
    public readonly float attackSpeedBonus;

    public PlayerPassiveUpgradeData(int stoneCost, int goldCost, float attackBonus, float attackSpeedBonus)
    {
        this.stoneCost = stoneCost;
        this.goldCost = goldCost;
        this.attackBonus = attackBonus;
        this.attackSpeedBonus = attackSpeedBonus;
    }
}

public struct WishStoneSkillUpgradeData
{
    public readonly int wishStoneCost;
    public readonly int goldCost;
    public readonly float skillDamageBonus;
    public readonly float cooldownReduction;

    public WishStoneSkillUpgradeData(int wishStoneCost, int goldCost, float skillDamageBonus, float cooldownReduction)
    {
        this.wishStoneCost = wishStoneCost;
        this.goldCost = goldCost;
        this.skillDamageBonus = skillDamageBonus;
        this.cooldownReduction = cooldownReduction;
    }
}
