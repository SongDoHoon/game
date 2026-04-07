using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Summon Table")]
public class SummonTable : ScriptableObject
{
    public int summonCost = 30;

    [Header("Grade Chance")]
    [Range(0, 100)] public float normalChance = 70;
    [Range(0, 100)] public float epicChance = 25;
    [Range(0, 100)] public float verureChance = 5;

    [Header("Units")]
    public List<WeightedUnitEntry> normalUnits = new();
    public List<WeightedUnitEntry> epicUnits = new();
    public List<WeightedUnitEntry> verureUnits = new();
}

[System.Serializable]
public class WeightedUnitEntry
{
    public UnitData unitData;
    public int weight = 1;
}