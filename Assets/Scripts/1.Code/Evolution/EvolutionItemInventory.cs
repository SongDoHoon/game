using System.Collections.Generic;
using UnityEngine;

public class EvolutionItemInventory : MonoBehaviour
{
    private readonly Dictionary<EvolutionItemType, int> itemCounts = new();

    public void AddItem(EvolutionItemType itemType, int amount = 1)
    {
        if (itemType == EvolutionItemType.None) return;

        if (!itemCounts.ContainsKey(itemType))
            itemCounts[itemType] = 0;

        itemCounts[itemType] += amount;
    }

    public bool HasItem(EvolutionItemType itemType, int amount = 1)
    {
        if (!itemCounts.ContainsKey(itemType)) return false;
        return itemCounts[itemType] >= amount;
    }

    public bool UseItem(EvolutionItemType itemType, int amount = 1)
    {
        if (!HasItem(itemType, amount)) return false;

        itemCounts[itemType] -= amount;
        if (itemCounts[itemType] <= 0)
            itemCounts.Remove(itemType);

        return true;
    }

    public int GetCount(EvolutionItemType itemType)
    {
        if (!itemCounts.ContainsKey(itemType)) return 0;
        return itemCounts[itemType];
    }
}