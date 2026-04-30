using System;
using System.Collections.Generic;

[Serializable]
public class UnitGrowthSaveData
{
    public List<UnitGrowthEntry> units = new();

    public UnitGrowthEntry GetOrCreateUnit(string unitId)
    {
        string safeUnitId = string.IsNullOrWhiteSpace(unitId) ? string.Empty : unitId;

        UnitGrowthEntry entry = units.Find(u => u.unitId == safeUnitId);
        if (entry != null)
            return entry;

        entry = new UnitGrowthEntry
        {
            unitId = safeUnitId,
            unitShardCount = 1,
            unitShardLevel = 1
        };
        units.Add(entry);
        return entry;
    }

    public UnitGrowthEntry GetUnit(string unitId)
    {
        string safeUnitId = string.IsNullOrWhiteSpace(unitId) ? string.Empty : unitId;
        return units.Find(u => u.unitId == safeUnitId);
    }
}

[Serializable]
public class UnitGrowthEntry
{
    public string unitId;
    public int unitShardCount;
    public int unitShardLevel;
    public int magicStoneCount;
    public int magicStoneLevel;
    public int equipmentLevel;
    public int relicCount;
    public int relicLevel;
    public int wishStoneCount;
    public int skillLevel;
}

[Serializable]
public class PlayerPassiveGrowthData
{
    public int enhancementStoneCount;
    public List<PlayerPassiveGrowthEntry> passives = new();

    public float totalAttackBonus;
    public float totalAttackSpeedBonus;

    public PlayerPassiveGrowthEntry GetOrCreatePassive(string passiveId)
    {
        string safePassiveId = string.IsNullOrWhiteSpace(passiveId) ? string.Empty : passiveId;

        PlayerPassiveGrowthEntry entry = passives.Find(p => p.passiveId == safePassiveId);
        if (entry != null)
            return entry;

        entry = new PlayerPassiveGrowthEntry
        {
            passiveId = safePassiveId
        };
        passives.Add(entry);
        return entry;
    }

    public PlayerPassiveGrowthEntry GetPassive(string passiveId)
    {
        string safePassiveId = string.IsNullOrWhiteSpace(passiveId) ? string.Empty : passiveId;
        return passives.Find(p => p.passiveId == safePassiveId);
    }
}

[Serializable]
public class PlayerPassiveGrowthEntry
{
    public string passiveId;
    public int level;
}

[Serializable]
public struct UnitStatModifierResult
{
    public double finalAttack;
    public float finalAttackInterval;
    public double baseAttack;
    public float baseAttackInterval;
    public double inGameGradeMultiplier;
    public double magicStoneMultiplier;
    public double equipmentMultiplier;
    public double relicMultiplier;
    public double additiveAttackBonus;
    public float attackSpeedMultiplier;
    public float additiveAttackSpeedBonus;
}
