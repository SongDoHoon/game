using System.Collections.Generic;
using UnityEngine;

public class EvolutionManager : MonoBehaviour
{
    public List<EvolutionRecipe> recipes = new();

    public EvolutionRecipe GetRecipe(UnitData baseUnit, EvolutionItemType itemType)
    {
        foreach (EvolutionRecipe recipe in recipes)
        {
            if (recipe.requiredBaseUnit == baseUnit && recipe.requiredItem == itemType)
                return recipe;
        }

        return null;
    }

    public EvolutionRecipe GetFirstRecipe(UnitData baseUnit)
    {
        foreach (EvolutionRecipe recipe in recipes)
        {
            if (recipe.requiredBaseUnit == baseUnit)
                return recipe;
        }

        return null;
    }

    public UnitData TryEvolve(UnitData baseUnit, EvolutionItemType itemType)
    {
        EvolutionRecipe recipe = GetRecipe(baseUnit, itemType);

        return recipe != null ? recipe.resultUnit : null;
    }

    public bool TryEvolveUnit(UnitController targetUnit, EvolutionItemType itemType)
    {
        if (targetUnit == null) return false;

        UnitData result = TryEvolve(targetUnit.Data, itemType);
        if (result == null) return false;

        targetUnit.Initialize(result);
        return true;
    }
}
