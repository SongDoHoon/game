using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    public SummonManager summonManager;
    public GameObject unitPrefab;
    public UnitPlacementTile[] placementTiles;

    private void Awake()
    {
        if (placementTiles == null || placementTiles.Length == 0)
            placementTiles = GetComponentsInChildren<UnitPlacementTile>(true);
    }

    public bool TryPlaceSummonedUnitOnTile(UnitPlacementTile tile)
    {
        if (tile == null || tile.IsOccupied) return false;
        if (summonManager == null || unitPrefab == null) return false;

        UnitData summonedData = summonManager.SummonUnit();
        if (summonedData == null) return false;

        return tile.PlaceNewUnit(unitPrefab, summonedData);
    }

    public bool TryPlaceSummonedUnitOnFirstEmptyTile()
    {
        if (placementTiles == null || placementTiles.Length == 0) return false;

        foreach (UnitPlacementTile tile in placementTiles)
        {
            if (!tile.IsOccupied)
                return TryPlaceSummonedUnitOnTile(tile);
        }

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
