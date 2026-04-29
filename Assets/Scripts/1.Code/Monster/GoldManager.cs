using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public int currentGold = 300;

    public void AddGold(int amount)
    {
        currentGold += amount;
    }

    public bool UseGold(int amount)
    {
        if (currentGold < amount)
            return false;

        currentGold -= amount;
        return true;
    }

    public bool TryEnhance(UnitEnhanceGroup group)
    {
        return GameModifierState.TryEnhance(group, this);
    }

    public int GetEnhanceCost(UnitEnhanceGroup group)
    {
        return GameModifierState.GetNextEnhancementCost(group);
    }

    public int GetReducedUnitExchangeCost(int baseCost)
    {
        return GameModifierState.GetReducedUnitExchangeCost(baseCost);
    }
}
