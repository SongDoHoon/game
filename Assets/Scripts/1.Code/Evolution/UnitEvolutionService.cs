using UnityEngine;

public class UnitEvolutionService : MonoBehaviour
{
    public EvolutionManager evolutionManager;
    public EvolutionItemInventory itemInventory;

    public bool TryEvolveUnit(UnitController unit, EvolutionItemType itemType)
    {
        if (unit == null) return false;
        if (evolutionManager == null || itemInventory == null) return false;

        if (!itemInventory.HasItem(itemType))
            return false;

        UnitData result = evolutionManager.TryEvolve(unit.Data, itemType);
        if (result == null)
            return false;

        itemInventory.UseItem(itemType, 1);
        unit.Initialize(result);
        return true;
    }
}