using System.Collections.Generic;
using UnityEngine;

public class SummonManager : MonoBehaviour
{
    public SummonTable summonTable;
    public GoldManager goldManager;

    public UnitData SummonUnit()
    {
        if (summonTable == null)
            return null;

        if (goldManager == null)
            return null;

        UnitGrade selectedGrade = RollGrade();
        List<WeightedUnitEntry> pool = GetPool(selectedGrade);

        if (pool == null || pool.Count == 0)
            return null;

        UnitData summonedUnit = RollUnit(pool);
        if (summonedUnit == null)
            return null;

        if (!goldManager.UseGold(summonTable.summonCost))
            return null;

        return summonedUnit;
    }

    private UnitGrade RollGrade()
    {
        float roll = Random.Range(0f, 100f);
        float cumulativeChance = summonTable.normalChance;

        if (roll < cumulativeChance)
            return UnitGrade.Normal;

        cumulativeChance += summonTable.rareChance;
        if (roll < cumulativeChance)
            return UnitGrade.Rare;

        cumulativeChance += summonTable.epicChance;
        if (roll < cumulativeChance)
            return UnitGrade.Epic;

        return UnitGrade.Verure;
    }

    private List<WeightedUnitEntry> GetPool(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.Normal => summonTable.normalUnits,
            UnitGrade.Rare => summonTable.rareUnits,
            UnitGrade.Epic => summonTable.epicUnits,
            UnitGrade.Verure => summonTable.verureUnits,
            _ => null
        };
    }

    private UnitData RollUnit(List<WeightedUnitEntry> pool)
    {
        int totalWeight = 0;

        foreach (WeightedUnitEntry entry in pool)
            totalWeight += entry.weight;

        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int sum = 0;

        foreach (WeightedUnitEntry entry in pool)
        {
            sum += entry.weight;

            if (roll < sum)
                return entry.unitData;
        }

        return pool[0].unitData;
    }
}
