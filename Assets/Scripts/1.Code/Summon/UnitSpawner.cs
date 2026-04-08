using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public UnitPlacementManager placementManager;

    public void SpawnSummonedUnit()
    {
        if (placementManager == null)
            return;

        placementManager.TryPlaceSummonedUnitOnFirstEmptyTile();
    }
}
