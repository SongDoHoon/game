using UnityEngine;
using UnityEngine.EventSystems;

public class UnitPlacementTile : MonoBehaviour
{
    [SerializeField] private Transform unitAnchor;
    private UnitController placedUnit;
    private UnitPlacementManager placementManager;

    public bool IsOccupied => placedUnit != null;
    public UnitController PlacedUnit => placedUnit;

    private void Awake()
    {
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (IsOccupied)
        {
            if (placementManager == null)
                placementManager = FindFirstObjectByType<UnitPlacementManager>();

            if (placementManager != null)
                placementManager.InspectUnit(placedUnit);

            return;
        }

        if (placementManager == null)
            placementManager = FindFirstObjectByType<UnitPlacementManager>();

        if (placementManager == null)
            return;

        placementManager.TryPlaceSummonedUnitOnTile(this);
    }

    public Vector3 GetPlacePosition()
    {
        if (unitAnchor != null)
            return unitAnchor.position;

        return transform.position;
    }

    public bool PlaceNewUnit(GameObject unitPrefab, UnitData data)
    {
        if (IsOccupied) return false;
        if (unitPrefab == null || data == null) return false;

        GameObject obj = Instantiate(unitPrefab, GetPlacePosition(), Quaternion.identity);
        UnitController unit = obj.GetComponent<UnitController>();

        if (unit == null)
        {
            Destroy(obj);
            return false;
        }

        placedUnit = unit;
        placedUnit.Initialize(data);
        placedUnit.SetTile(this);

        return true;
    }

    public bool PlaceExistingUnit(UnitController unit)
    {
        if (IsOccupied) return false;
        if (unit == null) return false;

        placedUnit = unit;
        placedUnit.transform.position = GetPlacePosition();
        placedUnit.SetTile(this);

        return true;
    }

    public void ClearTile()
    {
        placedUnit = null;
    }

    public void RemoveUnitFromTile()
    {
        if (placedUnit != null)
        {
            Destroy(placedUnit.gameObject);
            placedUnit = null;
        }
    }
}
