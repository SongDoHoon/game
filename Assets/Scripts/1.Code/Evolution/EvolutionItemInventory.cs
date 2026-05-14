using System.Collections.Generic;
using System;
using UnityEngine;

public class EvolutionItemInventory : MonoBehaviour
{
    public event Action<EvolutionItemType, int> OnItemCountChanged;

    private readonly Dictionary<EvolutionItemType, int> itemCounts = new();

    public void AddItem(EvolutionItemType itemType, int amount = 1)
    {
        if (itemType == EvolutionItemType.None) return;
        if (amount <= 0) return;

        if (!itemCounts.ContainsKey(itemType))
            itemCounts[itemType] = 0;

        itemCounts[itemType] += amount;
        OnItemCountChanged?.Invoke(itemType, itemCounts[itemType]);
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
        {
            itemCounts.Remove(itemType);
            OnItemCountChanged?.Invoke(itemType, 0);
        }
        else
        {
            OnItemCountChanged?.Invoke(itemType, itemCounts[itemType]);
        }

        return true;
    }

    public int GetCount(EvolutionItemType itemType)
    {
        if (!itemCounts.ContainsKey(itemType)) return 0;
        return itemCounts[itemType];
    }
}
