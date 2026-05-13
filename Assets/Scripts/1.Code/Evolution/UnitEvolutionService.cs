using UnityEngine;

public class UnitEvolutionService : MonoBehaviour
{
    public EvolutionManager evolutionManager;
    public EvolutionItemInventory itemInventory;

    private void Awake()
    {
        ResolveReferences();
    }

    public bool CanEvolveUnit(UnitController unit)
    {
        return TryGetAvailableRecipe(unit, out _);
    }

    public bool TryGetAvailableRecipe(UnitController unit, out EvolutionRecipe recipe)
    {
        recipe = null;
        ResolveReferences();

        if (unit == null) return false;
        if (unit.Data == null) return false;
        if (evolutionManager == null || itemInventory == null) return false;

        return evolutionManager.TryGetAvailableRecipe(unit.Data, itemInventory, out recipe);
    }

    public bool TryEvolveFirstAvailable(UnitController unit)
    {
        if (!TryGetAvailableRecipe(unit, out EvolutionRecipe recipe))
            return false;

        return TryEvolveUnit(unit, recipe.requiredItem);
    }

    public bool TryEvolveUnit(UnitController unit, EvolutionItemType itemType)
    {
        ResolveReferences();

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

    private void ResolveReferences()
    {
        if (evolutionManager == null)
            evolutionManager = FindFirstObjectByType<EvolutionManager>();

        if (itemInventory == null)
            itemInventory = FindFirstObjectByType<EvolutionItemInventory>();
    }
}
