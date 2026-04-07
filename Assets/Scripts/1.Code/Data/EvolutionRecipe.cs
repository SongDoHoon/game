using UnityEngine;

[CreateAssetMenu(menuName = "TD/Evolution Recipe")]
public class EvolutionRecipe : ScriptableObject
{
    public string evolveKey;
    public UnitData requiredBaseUnit;
    public EvolutionItemType requiredItem;
    public UnitData resultUnit;
}