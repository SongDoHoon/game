using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UnitPlacementManager : MonoBehaviour
{
    private static readonly Rect UnitPanelRect = new Rect(10f, 50f, 300f, 420f);

    public SummonManager summonManager;
    public GameObject unitPrefab;
    public UnitPlacementTile[] placementTiles;

    public static UnitPlacementManager Instance { get; private set; }

    private readonly List<UnitData> selectableUnits = new();
    private Vector2 unitScrollPosition;
    private UnitData selectedPlacementUnit;
    private bool showUnitSelectionUI = true;
    private bool showAllUnitRanges;
    private UnitController inspectedUnit;

    private void Awake()
    {
        Instance = this;

        if (placementTiles == null || placementTiles.Length == 0)
            placementTiles = GetComponentsInChildren<UnitPlacementTile>(true);

        RefreshSelectableUnits();
        RefreshRangeVisuals();
    }

    private void OnDestroy()
    {
        RefreshRangeVisuals(true);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        Vector2 worldPoint = targetCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hitCollider = Physics2D.OverlapPoint(worldPoint);

        if (hitCollider == null)
        {
            ClearInspectedUnit();
            return;
        }

        if (hitCollider.GetComponent<UnitController>() != null)
            return;

        if (hitCollider.GetComponent<UnitPlacementTile>() != null)
            return;

        ClearInspectedUnit();
    }

    public bool TryPlaceSummonedUnitOnTile(UnitPlacementTile tile)
    {
        if (tile == null || tile.IsOccupied) return false;
        if (unitPrefab == null) return false;

        if (selectedPlacementUnit != null)
            return tile.PlaceNewUnit(unitPrefab, selectedPlacementUnit);

        if (summonManager == null) return false;

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

    public void InspectUnit(UnitController unit)
    {
        if (unit == null)
        {
            ClearInspectedUnit();
            return;
        }

        if (inspectedUnit == unit)
        {
            ClearInspectedUnit();
            return;
        }

        inspectedUnit = unit;
        RefreshRangeVisuals();
    }

    public void ClearInspectedUnit()
    {
        if (inspectedUnit == null)
            return;

        inspectedUnit = null;
        RefreshRangeVisuals();
    }

    public bool ShouldShowRangeFor(UnitController unit)
    {
        if (unit == null)
            return false;

        return showAllUnitRanges || inspectedUnit == unit;
    }

    private void RefreshSelectableUnits()
    {
        selectableUnits.Clear();

        if (summonManager != null && summonManager.summonTable != null)
        {
            AddUnitsFromEntries(summonManager.summonTable.normalUnits);
            AddUnitsFromEntries(summonManager.summonTable.rareUnits);
            AddUnitsFromEntries(summonManager.summonTable.epicUnits);
            AddUnitsFromEntries(summonManager.summonTable.verureUnits);
        }

        EvolutionManager evolutionManager = FindFirstObjectByType<EvolutionManager>();
        if (evolutionManager != null)
        {
            foreach (EvolutionRecipe recipe in evolutionManager.recipes)
            {
                if (recipe == null) continue;

                AddSelectableUnit(recipe.requiredBaseUnit);
                AddSelectableUnit(recipe.resultUnit);
            }
        }

        selectableUnits.Sort(CompareUnitData);
    }

    private void AddUnitsFromEntries(List<WeightedUnitEntry> entries)
    {
        if (entries == null)
            return;

        foreach (WeightedUnitEntry entry in entries)
        {
            if (entry == null) continue;
            AddSelectableUnit(entry.unitData);
        }
    }

    private void AddSelectableUnit(UnitData unitData)
    {
        if (unitData == null)
            return;

        if (selectableUnits.Contains(unitData))
            return;

        selectableUnits.Add(unitData);
    }

    private int CompareUnitData(UnitData left, UnitData right)
    {
        if (left == right)
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        int gradeCompare = ((int)left.grade).CompareTo((int)right.grade);
        if (gradeCompare != 0)
            return gradeCompare;

        bool leftIdParsed = int.TryParse(left.unitId, out int leftId);
        bool rightIdParsed = int.TryParse(right.unitId, out int rightId);

        if (leftIdParsed && rightIdParsed)
        {
            int idCompare = leftId.CompareTo(rightId);
            if (idCompare != 0)
                return idCompare;
        }

        return string.Compare(left.unitName, right.unitName, System.StringComparison.Ordinal);
    }

    private void OnGUI()
    {
        DrawUnitSelectionToggle();
        DrawSelectedUnitOverlay();

        if (!showUnitSelectionUI)
            return;

        GUILayout.BeginArea(UnitPanelRect, "Unit Placement", GUI.skin.window);
        GUILayout.Label(selectedPlacementUnit != null
            ? $"Selected: {selectedPlacementUnit.unitName}"
            : "Selected: Random Summon");
        GUILayout.Label(selectedPlacementUnit != null
            ? "Empty tile click: selected unit direct placement"
            : "Empty tile click: summon table random placement");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Random Summon Mode", GUILayout.Height(28f)))
            selectedPlacementUnit = null;

        if (GUILayout.Button("Refresh List", GUILayout.Height(28f)))
            RefreshSelectableUnits();

        GUILayout.EndHorizontal();

        bool nextShowAllRanges = GUILayout.Toggle(showAllUnitRanges, "Show All Unit Ranges");
        if (nextShowAllRanges != showAllUnitRanges)
        {
            showAllUnitRanges = nextShowAllRanges;
            RefreshRangeVisuals();
        }

        if (GUILayout.Button("Clear Range Focus", GUILayout.Height(24f)))
            ClearInspectedUnit();

        GUILayout.Label(inspectedUnit != null
            ? $"Range Focus: {(inspectedUnit.Data != null ? inspectedUnit.Data.unitName : inspectedUnit.name)}"
            : "Range Focus: None");

        unitScrollPosition = GUILayout.BeginScrollView(unitScrollPosition, GUILayout.ExpandHeight(true));

        foreach (UnitData unitData in selectableUnits)
        {
            if (unitData == null)
                continue;

            string label = $"[{unitData.grade}] {unitData.unitName}";
            if (GUILayout.Button(label, GUILayout.Height(28f)))
                selectedPlacementUnit = unitData;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawUnitSelectionToggle()
    {
        Rect toggleRect = new Rect(10f, 10f, 150f, 30f);
        string buttonLabel = showUnitSelectionUI ? "Hide Unit UI" : "Show Unit UI";

        if (GUI.Button(toggleRect, buttonLabel))
            showUnitSelectionUI = !showUnitSelectionUI;
    }

    private void DrawSelectedUnitOverlay()
    {
        if (inspectedUnit == null)
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        bool canMerge = TryGetMergeInfo(inspectedUnit, out UnitController materialUnit, out UnitData mergeResult, out string reason);
        Vector3 worldPosition = inspectedUnit.transform.position + new Vector3(0f, 1.35f, 0f);
        Vector3 screenPoint = targetCamera.WorldToScreenPoint(worldPosition);

        if (screenPoint.z <= 0f)
            return;

        float guiX = screenPoint.x - 90f;
        float guiY = Screen.height - screenPoint.y - 44f;
        Rect overlayRect = new Rect(guiX, guiY, 180f, 72f);

        if (showUnitSelectionUI && overlayRect.Overlaps(UnitPanelRect))
            guiX = UnitPanelRect.xMax + 12f;

        overlayRect.x = Mathf.Clamp(guiX, 0f, Mathf.Max(0f, Screen.width - overlayRect.width));
        overlayRect.y = Mathf.Clamp(guiY, 0f, Mathf.Max(0f, Screen.height - overlayRect.height));

        GUILayout.BeginArea(overlayRect, GUI.skin.window);
        GUILayout.Label(canMerge
            ? $"Merge -> {GetUnitDisplayName(mergeResult)}"
            : reason);

        bool previousGuiEnabled = GUI.enabled;
        GUI.enabled = canMerge && materialUnit != null && mergeResult != null;

        if (GUILayout.Button("Merge", GUILayout.Height(28f)))
            TryMergeUnits(inspectedUnit, materialUnit, mergeResult);

        GUI.enabled = previousGuiEnabled;
        GUILayout.EndArea();
    }

    private bool TryGetMergeInfo(UnitController baseUnit, out UnitController materialUnit, out UnitData mergeResult, out string reason)
    {
        materialUnit = null;
        mergeResult = null;
        reason = "Not available";

        if (baseUnit == null || baseUnit.Data == null)
        {
            reason = "No unit selected";
            return false;
        }

        if (IsRestrictedMergeUnit(baseUnit.Data))
        {
            reason = "Angel/Demon units evolve with items";
            return false;
        }

        materialUnit = FindMatchingMergeMaterial(baseUnit);
        if (materialUnit == null)
        {
            reason = "Need one more identical unit";
            return false;
        }

        mergeResult = RollNextGradeMergeUnit(baseUnit.Data);
        if (mergeResult == null)
        {
            reason = "No next-grade unit found";
            return false;
        }

        return true;
    }

    private bool TryMergeUnits(UnitController baseUnit, UnitController materialUnit, UnitData mergeResult)
    {
        if (baseUnit == null || materialUnit == null || mergeResult == null)
            return false;

        if (baseUnit == materialUnit)
            return false;

        UnitPlacementTile materialTile = materialUnit.CurrentTile;
        if (materialTile == null)
            return false;

        materialTile.RemoveUnitFromTile();
        baseUnit.Initialize(mergeResult);
        InspectUnit(baseUnit);
        return true;
    }

    private UnitController FindMatchingMergeMaterial(UnitController baseUnit)
    {
        if (baseUnit == null || baseUnit.Data == null)
            return null;

        UnitController[] units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        foreach (UnitController candidate in units)
        {
            if (candidate == null || candidate == baseUnit)
                continue;

            if (candidate.Data == baseUnit.Data)
                return candidate;
        }

        return null;
    }

    private UnitData RollNextGradeMergeUnit(UnitData currentUnit)
    {
        if (currentUnit == null)
            return null;

        UnitGrade? nextGrade = GetNextMergeGrade(currentUnit.grade);
        if (!nextGrade.HasValue)
            return null;

        List<WeightedUnitEntry> mergePool = GetMergePool(nextGrade.Value);
        if (mergePool == null || mergePool.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (WeightedUnitEntry entry in mergePool)
        {
            if (entry == null || entry.unitData == null)
                continue;

            if (IsRestrictedMergeUnit(entry.unitData))
                continue;

            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (WeightedUnitEntry entry in mergePool)
        {
            if (entry == null || entry.unitData == null)
                continue;

            if (IsRestrictedMergeUnit(entry.unitData))
                continue;

            cumulativeWeight += Mathf.Max(0, entry.weight);

            if (roll < cumulativeWeight)
                return entry.unitData;
        }

        return null;
    }

    private UnitGrade? GetNextMergeGrade(UnitGrade currentGrade)
    {
        switch (currentGrade)
        {
            case UnitGrade.Normal:
                return UnitGrade.Rare;

            case UnitGrade.Rare:
                return UnitGrade.Epic;

            case UnitGrade.Epic:
                return UnitGrade.Verure;

            default:
                return null;
        }
    }

    private List<WeightedUnitEntry> GetMergePool(UnitGrade grade)
    {
        if (summonManager == null || summonManager.summonTable == null)
            return null;

        switch (grade)
        {
            case UnitGrade.Rare:
                return summonManager.summonTable.rareUnits;

            case UnitGrade.Epic:
                return summonManager.summonTable.epicUnits;

            case UnitGrade.Verure:
                return summonManager.summonTable.verureUnits;

            default:
                return null;
        }
    }

    private bool IsRestrictedMergeUnit(UnitData unitData)
    {
        if (unitData == null)
            return true;

        return unitData.grade == UnitGrade.ArchAngel
            || unitData.grade == UnitGrade.GreatDemon
            || unitData.specialLogicType != SpecialUnitLogicType.None;
    }

    private string GetUnitDisplayName(UnitData unitData)
    {
        if (unitData == null)
            return "None";

        if (!string.IsNullOrWhiteSpace(unitData.unitName))
            return unitData.unitName;

        return unitData.name;
    }

    private void RefreshRangeVisuals(bool clearOnly = false)
    {
        UnitController[] units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        foreach (UnitController unit in units)
        {
            if (unit == null)
                continue;

            bool shouldShow = !clearOnly && (showAllUnitRanges || unit == inspectedUnit);
            unit.SetSelectionVisualActive(shouldShow);
        }
    }

}
