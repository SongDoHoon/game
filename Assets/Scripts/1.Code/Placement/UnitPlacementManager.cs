using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    public SummonManager summonManager;
    public GameObject unitPrefab;
    public UnitPlacementTile[] placementTiles;

    public bool TryPlaceSummonedUnitOnTile(UnitPlacementTile tile)
    {
        if (tile == null || tile.IsOccupied) return false;
        if (summonManager == null || unitPrefab == null) return false;

        UnitData summonedData = summonManager.SummonUnit();
        if (summonedData == null) return false;

        bool success = tile.PlaceNewUnit(unitPrefab, summonedData);

        if (success)
        {
            Debug.Log($"타일 [{tile.name}] 에 [{summonedData.unitName}] 배치 완료");
        }

        return success;
    }

    public bool TryPlaceSummonedUnitOnFirstEmptyTile()
    {
        if (placementTiles == null || placementTiles.Length == 0) return false;

        foreach (UnitPlacementTile tile in placementTiles)
        {
            if (!tile.IsOccupied)
                return TryPlaceSummonedUnitOnTile(tile);
        }

        Debug.Log("빈 타일이 없음");
        return false;
    }

    public bool TryMoveUnit(UnitPlacementTile fromTile, UnitPlacementTile toTile)
    {
        if (fromTile == null || toTile == null) return false;
        if (!fromTile.IsOccupied || toTile.IsOccupied) return false;

        UnitController unit = fromTile.PlacedUnit;
        fromTile.ClearTile();
        return toTile.PlaceExistingUnit(unit);
    }
}
