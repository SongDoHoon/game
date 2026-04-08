using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public UnitPlacementManager placementManager;

    public void SpawnSummonedUnit()
    {
        if (placementManager == null)
        {
            Debug.LogWarning("UnitPlacementManager가 연결되지 않음");
            return;
        }

        bool success = placementManager.TryPlaceSummonedUnitOnFirstEmptyTile();

        if (success)
            Debug.Log("유닛 배치 성공");
        else
            Debug.Log("유닛 배치 실패");
    }
}
