using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class UnitPlacementManager : MonoBehaviour
{
    private static readonly Rect UnitPanelRect = new Rect(10f, 50f, 300f, 420f);

    public SummonManager summonManager;
    public GoldManager goldManager;
    public GameObject unitPrefab;
    public UnitPlacementTile[] placementTiles;

    [Header("Merge UI")]
    public GameObject mergeButtonRoot;
    public Button mergeButton;
    public TMP_Text mergeButtonText;
    public bool positionMergeButtonNearSelectedUnit = true;
    public Vector2 mergeButtonScreenOffset = new Vector2(0f, -70f);

    public static UnitPlacementManager Instance { get; private set; }

    private readonly List<UnitData> selectableUnits = new();
    private Vector2 unitScrollPosition;
    private UnitData selectedPlacementUnit;
    private bool showUnitSelectionUI = true;
    private bool showAllUnitRanges;
    private UnitController inspectedUnit;
    private RectTransform mergeButtonRectTransform;
    private UnitEvolutionService evolutionService;

    private void Awake()
    {
        Instance = this;

        if (placementTiles == null || placementTiles.Length == 0)
            placementTiles = GetComponentsInChildren<UnitPlacementTile>(true);

        if (goldManager == null)
            goldManager = FindFirstObjectByType<GoldManager>();

        ResolveEvolutionService();
        RefreshSelectableUnits();
        RefreshRangeVisuals();
        BindMergeButtonEvent();
        RefreshMergeUI();
    }

    private void OnDestroy()
    {
        RefreshRangeVisuals(true);

        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        UpdateMergeButtonPosition();
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

    public bool TryExchangeInspectedUnit()
    {
        if (inspectedUnit == null || inspectedUnit.Data == null)
        {
            RefreshMergeUI();
            return false;
        }

        bool success = TryExchangeUnit(inspectedUnit);
        RefreshRangeVisuals();
        RefreshMergeUI();
        return success;
    }

    public bool TryEvolveInspectedUnit()
    {
        if (inspectedUnit == null || inspectedUnit.Data == null)
        {
            RefreshMergeUI();
            return false;
        }

        ResolveEvolutionService();

        bool success = evolutionService != null && evolutionService.TryEvolveFirstAvailable(inspectedUnit);
        RefreshRangeVisuals();
        RefreshMergeUI();
        return success;
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
        RefreshMergeUI();
    }

    public void ClearInspectedUnit()
    {
        if (inspectedUnit == null)
            return;

        inspectedUnit = null;
        RefreshRangeVisuals();
        RefreshMergeUI();
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

        UnitGrowthManager unitGrowthManager = UnitGrowthManager.Instance;
        if (unitGrowthManager == null)
            unitGrowthManager = FindFirstObjectByType<UnitGrowthManager>();

        if (unitGrowthManager != null)
            AddUnitsFromDatabase(unitGrowthManager.unitDatabase);

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

    private void AddUnitsFromDatabase(UnitData[] unitDatabase)
    {
        if (unitDatabase == null)
            return;

        foreach (UnitData unitData in unitDatabase)
            AddSelectableUnit(unitData);
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

    public void TryMergeInspectedUnit()
    {
        if (inspectedUnit == null)
        {
            RefreshMergeUI();
            return;
        }

        if (!TryGetMergeInfo(inspectedUnit, out UnitController materialUnit, out _, out _))
        {
            RefreshMergeUI();
            return;
        }

        UnitData mergeResult = RollNextGradeMergeUnit(inspectedUnit.Data);
        if (mergeResult == null)
        {
            RefreshMergeUI();
            return;
        }

        if (TryMergeUnits(inspectedUnit, materialUnit, mergeResult))
            RefreshMergeUI();
    }

    public void TryUseInspectedUnitAction()
    {
        if (inspectedUnit == null)
        {
            RefreshMergeUI();
            return;
        }

        if (inspectedUnit.HasManualSelfEnhancement())
        {
            TryManualEnhanceInspectedUnit();
            return;
        }

        if (CanEvolveUnit(inspectedUnit))
        {
            TryEvolveInspectedUnit();
            return;
        }

        if (TryGetMergeInfo(inspectedUnit, out _, out _, out _))
        {
            TryMergeInspectedUnit();
            return;
        }

        TryExchangeInspectedUnit();
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
        GUILayout.Label(canMerge ? "Merge" : reason);

        bool previousGuiEnabled = GUI.enabled;
        GUI.enabled = canMerge && materialUnit != null;

        if (GUILayout.Button("Merge", GUILayout.Height(28f)))
            TryMergeInspectedUnit();

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

        if (!HasNextGradeMergePool(baseUnit.Data))
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
        RefreshRangeVisuals();
        RefreshMergeUI();
        return true;
    }

    private bool TryExchangeUnit(UnitController targetUnit)
    {
        if (targetUnit == null || targetUnit.Data == null)
            return false;

        int exchangeCost = GetUnitExchangeCost(targetUnit.Data);
        if (exchangeCost < 0)
            return false;

        UnitData exchangeResult = RollSameGradeExchangeUnit(targetUnit.Data);
        if (exchangeResult == null)
            return false;

        BattleMagicStoneManager magicStoneManager = BattleMagicStoneManager.Instance;
        if (magicStoneManager == null)
            magicStoneManager = FindFirstObjectByType<BattleMagicStoneManager>();

        if (magicStoneManager == null || !magicStoneManager.TrySpendBattleMagicStone(exchangeCost))
            return false;

        targetUnit.Initialize(exchangeResult);
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

        UnitGrade? nextGrade = RollMergeResultGrade(currentUnit.grade);
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

    private UnitData RollSameGradeExchangeUnit(UnitData currentUnit)
    {
        if (currentUnit == null || !GameBalanceConfig.CanExchangeUnitGrade(currentUnit.grade))
            return null;

        List<WeightedUnitEntry> exchangePool = GetMergePool(currentUnit.grade);
        if (exchangePool == null || exchangePool.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (WeightedUnitEntry entry in exchangePool)
        {
            if (!IsValidExchangeCandidate(entry, currentUnit))
                continue;

            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (WeightedUnitEntry entry in exchangePool)
        {
            if (!IsValidExchangeCandidate(entry, currentUnit))
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

    private UnitGrade? RollMergeResultGrade(UnitGrade currentGrade)
    {
        UnitGrade? nextGrade = GetNextMergeGrade(currentGrade);
        if (!nextGrade.HasValue)
            return null;

        if (UnityEngine.Random.value > GameModifierState.MergeTwoGradeUpChance)
            return nextGrade;

        UnitGrade? twoGradeUp = GetNextMergeGrade(nextGrade.Value);
        return twoGradeUp ?? nextGrade;
    }

    private List<WeightedUnitEntry> GetMergePool(UnitGrade grade)
    {
        if (summonManager == null || summonManager.summonTable == null)
            return null;

        switch (grade)
        {
            case UnitGrade.Normal:
                return summonManager.summonTable.normalUnits;

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
            || unitData.grade == UnitGrade.GreatDemon;
    }

    private bool CanExchangeUnit(UnitData unitData)
    {
        if (unitData == null)
            return false;

        if (!GameBalanceConfig.CanExchangeUnitGrade(unitData.grade))
            return false;

        if (!HasExchangeCandidate(unitData))
            return false;

        int exchangeCost = GetUnitExchangeCost(unitData);
        BattleMagicStoneManager magicStoneManager = BattleMagicStoneManager.Instance;
        if (magicStoneManager == null)
            magicStoneManager = FindFirstObjectByType<BattleMagicStoneManager>();

        return magicStoneManager != null && magicStoneManager.CanSpendBattleMagicStone(exchangeCost);
    }

    private bool HasExchangeCandidate(UnitData currentUnit)
    {
        if (currentUnit == null || !GameBalanceConfig.CanExchangeUnitGrade(currentUnit.grade))
            return false;

        List<WeightedUnitEntry> exchangePool = GetMergePool(currentUnit.grade);
        if (exchangePool == null || exchangePool.Count == 0)
            return false;

        foreach (WeightedUnitEntry entry in exchangePool)
        {
            if (IsValidExchangeCandidate(entry, currentUnit))
                return true;
        }

        return false;
    }

    private int GetUnitExchangeCost(UnitData unitData)
    {
        if (unitData == null)
            return GameBalanceConfig.UnitExchangeUnavailableCost;

        int baseCost = GameBalanceConfig.GetUnitExchangeBaseCost(unitData.grade);
        return GameModifierState.GetReducedUnitExchangeCost(baseCost);
    }

    private bool IsValidExchangeCandidate(WeightedUnitEntry entry, UnitData currentUnit)
    {
        if (entry == null || entry.unitData == null || currentUnit == null)
            return false;

        if (IsSameUnitData(entry.unitData, currentUnit))
            return false;

        if (entry.unitData.grade != currentUnit.grade)
            return false;

        if (!GameBalanceConfig.CanExchangeUnitGrade(entry.unitData.grade))
            return false;

        return entry.weight > 0;
    }

    private bool IsSameUnitData(UnitData left, UnitData right)
    {
        if (left == right)
            return true;

        return left != null
            && right != null
            && !string.IsNullOrEmpty(left.unitId)
            && left.unitId == right.unitId;
    }

    private bool CanEvolveUnit(UnitController unit)
    {
        if (unit == null || unit.Data == null)
            return false;

        ResolveEvolutionService();
        return evolutionService != null && evolutionService.TryGetAvailableRecipe(unit, out _);
    }

    private void ResolveEvolutionService()
    {
        if (evolutionService == null)
            evolutionService = FindFirstObjectByType<UnitEvolutionService>();
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

    private void BindMergeButtonEvent()
    {
        if (mergeButton == null)
            return;

        mergeButton.onClick.RemoveListener(TryMergeInspectedUnit);
        mergeButton.onClick.RemoveListener(TryUseInspectedUnitAction);
        mergeButton.onClick.AddListener(TryUseInspectedUnitAction);
    }

    private void RefreshMergeUI()
    {
        if (mergeButtonRoot == null)
            return;

        if (inspectedUnit == null)
        {
            mergeButtonRoot.SetActive(false);
            return;
        }

        if (inspectedUnit.HasManualSelfEnhancement())
        {
            mergeButtonRoot.SetActive(true);
            RefreshManualEnhanceUI();
            return;
        }

        bool canEvolve = CanEvolveUnit(inspectedUnit);
        bool canMerge = TryGetMergeInfo(inspectedUnit, out _, out _, out _);
        bool canExchange = CanExchangeUnit(inspectedUnit.Data);
        mergeButtonRoot.SetActive(canEvolve || canMerge || canExchange);

        if (!canEvolve && !canMerge && !canExchange)
            return;

        if (mergeButton != null)
            mergeButton.interactable = true;

        if (mergeButtonText != null)
        {
            if (canEvolve)
                mergeButtonText.text = "\uC9C4\uD654";
            else if (canMerge)
                mergeButtonText.text = "Merge";
            else
                mergeButtonText.text = $"\uAD50\uD658 {GetUnitExchangeCost(inspectedUnit.Data)}";
        }

        UpdateMergeButtonPosition();
    }

    private void RefreshManualEnhanceUI()
    {
        if (inspectedUnit == null)
            return;

        if (mergeButton != null)
            mergeButton.interactable = inspectedUnit.CanTryManualEnhance(goldManager);

        if (mergeButtonText != null)
        {
            int stack = inspectedUnit.GetManualEnhanceStack();
            int maxStack = inspectedUnit.GetManualEnhanceMaxStack();
            int cost = inspectedUnit.GetManualEnhanceCost();
            int chancePercent = Mathf.RoundToInt(inspectedUnit.GetManualEnhanceSuccessChance() * 100f);
            mergeButtonText.text = maxStack > 0 && stack >= maxStack
                ? $"Enhance MAX ({stack}/{maxStack})"
                : $"Enhance {cost}G {chancePercent}% ({stack}/{maxStack})";
        }

        UpdateMergeButtonPosition();
    }

    private void TryManualEnhanceInspectedUnit()
    {
        if (inspectedUnit == null)
        {
            RefreshMergeUI();
            return;
        }

        ManualEnhanceResult result = inspectedUnit.TryManualEnhance(goldManager);
        switch (result)
        {
            case ManualEnhanceResult.Success:
                Debug.Log($"{GetUnitDisplayName(inspectedUnit.Data)} enhancement succeeded. Stack: {inspectedUnit.GetManualEnhanceStack()}");
                break;

            case ManualEnhanceResult.Failed:
                Debug.Log($"{GetUnitDisplayName(inspectedUnit.Data)} enhancement failed.");
                break;

            case ManualEnhanceResult.NotEnoughGold:
                Debug.Log($"{GetUnitDisplayName(inspectedUnit.Data)} enhancement failed: not enough gold.");
                break;

            case ManualEnhanceResult.MaxStack:
                Debug.Log($"{GetUnitDisplayName(inspectedUnit.Data)} enhancement is already maxed.");
                break;
        }

        RefreshRangeVisuals();
        RefreshMergeUI();
    }

    private bool HasNextGradeMergePool(UnitData currentUnit)
    {
        if (currentUnit == null)
            return false;

        UnitGrade? nextGrade = GetNextMergeGrade(currentUnit.grade);
        if (!nextGrade.HasValue)
            return false;

        if (HasAvailableMergeUnitInPool(nextGrade.Value))
            return true;

        UnitGrade? twoGradeUp = GetNextMergeGrade(nextGrade.Value);
        return twoGradeUp.HasValue && HasAvailableMergeUnitInPool(twoGradeUp.Value);
    }

    private bool HasAvailableMergeUnitInPool(UnitGrade grade)
    {
        List<WeightedUnitEntry> mergePool = GetMergePool(grade);
        if (mergePool == null || mergePool.Count == 0)
            return false;

        foreach (WeightedUnitEntry entry in mergePool)
        {
            if (entry == null || entry.unitData == null)
                continue;

            if (IsRestrictedMergeUnit(entry.unitData))
                continue;

            if (entry.weight > 0)
                return true;
        }

        return false;
    }

    private void UpdateMergeButtonPosition()
    {
        if (!positionMergeButtonNearSelectedUnit)
            return;

        if (mergeButtonRoot == null || inspectedUnit == null)
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (mergeButtonRectTransform == null)
            mergeButtonRectTransform = mergeButtonRoot.GetComponent<RectTransform>();

        if (mergeButtonRectTransform == null)
            return;

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(inspectedUnit.transform.position);
        if (screenPosition.z <= 0f)
            return;

        mergeButtonRectTransform.position = (Vector2)screenPosition + mergeButtonScreenOffset;
    }

}
