using UnityEngine;

public class UnitEvolutionService : MonoBehaviour
{
    public EvolutionManager evolutionManager;
    public EvolutionItemInventory itemInventory;

    public bool CanEvolveUnit(UnitController unit)
    {
        return TryGetAvailableRecipe(unit, out _);
    }

    public bool TryGetAvailableRecipe(UnitController unit, out EvolutionRecipe recipe)
    {
        recipe = null;

        if (unit == null) return false;
        if (unit.Data == null) return false;
        if (evolutionManager == null || itemInventory == null) return false;

        foreach (EvolutionRecipe candidate in evolutionManager.recipes)
        {
            if (candidate == null) continue;
            if (candidate.requiredBaseUnit != unit.Data) continue;
            if (!itemInventory.HasItem(candidate.requiredItem)) continue;

            recipe = candidate;
            return true;
        }

        return false;
    }

    public bool TryEvolveFirstAvailable(UnitController unit)
    {
        if (!TryGetAvailableRecipe(unit, out EvolutionRecipe recipe))
            return false;

        return TryEvolveUnit(unit, recipe.requiredItem);
    }

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
