using System.Collections.Generic;
using UnityEngine;

public class SummonManager : MonoBehaviour
{
    public SummonTable summonTable;
    public GoldManager goldManager;

    public UnitData SummonUnit()
    {
        if (summonTable == null)
        {
            Debug.LogWarning("SummonTable이 연결되지 않음");
            return null;
        }

        if (goldManager == null)
        {
            Debug.LogWarning("GoldManager가 연결되지 않음");
            return null;
        }

        if (!goldManager.UseGold(summonTable.summonCost))
        {
            return null;
        }

        UnitGrade selectedGrade = RollGrade();
        List<WeightedUnitEntry> pool = GetPool(selectedGrade);

        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("해당 등급 풀에 유닛이 없음");
            return null;
        }

        UnitData result = RollUnit(pool);

        if (result != null)
        {
            Debug.Log($"소환 결과: [{result.grade}] {result.unitName}");
        }

        return result;
    }

    private UnitGrade RollGrade()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < summonTable.normalChance)
            return UnitGrade.Normal;

        if (roll < summonTable.normalChance + summonTable.epicChance)
            return UnitGrade.Epic;

        return UnitGrade.Verure;
    }

    private List<WeightedUnitEntry> GetPool(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.Normal => summonTable.normalUnits,
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