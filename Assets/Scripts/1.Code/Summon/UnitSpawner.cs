using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public UnitPlacementManager placementManager;
    public bool allowSequentialSpawnFallback;

    public void SpawnSummonedUnit()
    {
        if (placementManager == null)
            return;

        if (!allowSequentialSpawnFallback)
            return;

        placementManager.TryPlaceSummonedUnitOnFirstEmptyTile();
    }
}
