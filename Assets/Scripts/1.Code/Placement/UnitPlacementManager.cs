using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class UnitPlacementManager : MonoBehaviour
{
    public SummonManager summonManager;
    public GameObject unitPrefab;
    public UnitPlacementTile[] placementTiles;

    [Header("Merge UI")]
    public GameObject mergeButtonRoot;
    public Button mergeButton;
    public TMP_Text mergeButtonText;
    public bool positionMergeButtonNearSelectedUnit = true;
    public Vector2 mergeButtonScreenOffset = new Vector2(0f, -70f);
    public Vector2 exchangeButtonScreenOffset = new Vector2(0f, 70f);

    [Header("Action Button Labels")]
    public string evolveButtonLabel = "Evolve";
    public string mergeButtonLabel = "Merge";
    public string exchangeButtonLabel = "Exchange";
    public string exchangeUnavailableLabel = "Exchange N/A";
    public TMP_FontAsset actionButtonFontAsset;

    [Header("Action Button Visuals")]
    [Range(0f, 1f)] public float enabledButtonAlpha = 1f;
    [Range(0f, 1f)] public float disabledButtonAlpha = 0.7f;

    public static UnitPlacementManager Instance { get; private set; }

    private UnitController inspectedUnit;
    private RectTransform mergeButtonRectTransform;
    private CanvasGroup mergeButtonCanvasGroup;
    private GameObject exchangeButtonRoot;
    private Button exchangeButton;
    private TMP_Text exchangeButtonText;
    private RectTransform exchangeButtonRectTransform;
    private CanvasGroup exchangeButtonCanvasGroup;
    private UnitEvolutionService evolutionService;
    private GoldManager goldManager;

    private void Awake()
    {
        Instance = this;

        if (placementTiles == null || placementTiles.Length == 0)
            placementTiles = GetComponentsInChildren<UnitPlacementTile>(true);

        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();

        ResolveEvolutionService();
        RefreshRangeVisuals();
        CreateExchangeButtonUI();
        BindMergeButtonEvent();
        BindExchangeButtonEvent();
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
        RefreshMergeUI();
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

    public IReadOnlyList<UnitController> GetPlacedUnits()
    {
        List<UnitController> units = new();

        if (placementTiles == null)
            return units;

        foreach (UnitPlacementTile tile in placementTiles)
        {
            if (tile != null && tile.PlacedUnit != null)
                units.Add(tile.PlacedUnit);
        }

        return units;
    }

    public bool TryMoveUnit(UnitPlacementTile fromTile, UnitPlacementTile toTile)
    {
        if (fromTile == null || toTile == null) return false;
        if (!fromTile.IsOccupied || toTile.IsOccupied) return false;

        UnitController unit = fromTile.PlacedUnit;
        fromTile.ClearTile();
        bool moved = toTile.PlaceExistingUnit(unit);
        NotifyMissionFieldUnitsChanged();
        return moved;
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
        if (success)
            NotifyMissionFieldUnitsChanged();

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

        return inspectedUnit == unit;
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
        {
            NotifyMissionFieldUnitsChanged();
            RefreshMergeUI();
        }
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
            if (inspectedUnit.CanTryManualEnhance(goldManager))
                TryManualEnhanceInspectedUnit();
            else
                RefreshMergeUI();

            return;
        }

        if (HasEvolutionRecipe(inspectedUnit) || IsRestrictedMergeUnit(inspectedUnit.Data))
        {
            if (CanEvolveUnit(inspectedUnit))
                TryEvolveInspectedUnit();
            else
                RefreshMergeUI();

            return;
        }

        if (TryGetMergeInfo(inspectedUnit, out _, out _, out _))
        {
            TryMergeInspectedUnit();
            return;
        }

        RefreshMergeUI();
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
            magicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        if (magicStoneManager == null || !magicStoneManager.TrySpendBattleMagicStone(exchangeCost))
            return false;

        targetUnit.Initialize(exchangeResult);
        NotifyMissionFieldUnitsChanged();
        return true;
    }

    private UnitController FindMatchingMergeMaterial(UnitController baseUnit)
    {
        if (baseUnit == null || baseUnit.Data == null)
            return null;

        UnitController[] units = FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

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

        return nextGrade;
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
            magicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

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

        return GameBalanceConfig.GetUnitExchangeBaseCost(unitData.grade);
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

    private bool HasEvolutionRecipe(UnitController unit)
    {
        if (unit == null || unit.Data == null)
            return false;

        EvolutionManager manager = null;
        ResolveEvolutionService();

        if (evolutionService != null)
            manager = evolutionService.evolutionManager;

        if (manager == null)
            manager = FindAnyObjectByType<EvolutionManager>();

        return manager != null && manager.GetFirstRecipe(unit.Data) != null;
    }

    private void ResolveEvolutionService()
    {
        if (evolutionService == null)
            evolutionService = FindAnyObjectByType<UnitEvolutionService>();
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
        UnitController[] units = FindObjectsByType<UnitController>(FindObjectsInactive.Exclude);

        foreach (UnitController unit in units)
        {
            if (unit == null)
                continue;

            bool shouldShow = !clearOnly && unit == inspectedUnit;
            unit.SetSelectionVisualActive(shouldShow);
        }
    }

    private void BindMergeButtonEvent()
    {
        if (mergeButton == null)
            return;

        mergeButtonCanvasGroup = GetOrAddCanvasGroup(mergeButtonRoot);
        mergeButton.onClick.RemoveListener(TryMergeInspectedUnit);
        mergeButton.onClick.RemoveListener(TryUseInspectedUnitAction);
        mergeButton.onClick.AddListener(TryUseInspectedUnitAction);
    }

    private void CreateExchangeButtonUI()
    {
        if (exchangeButtonRoot != null || mergeButtonRoot == null)
            return;

        Transform buttonParent = mergeButtonRoot.transform.parent;
        Transform existingExchangeRoot = buttonParent != null ? buttonParent.Find("exchangeButtonRoot") : null;
        exchangeButtonRoot = existingExchangeRoot != null
            ? existingExchangeRoot.gameObject
            : Instantiate(mergeButtonRoot, buttonParent);

        exchangeButtonRoot.name = "exchangeButtonRoot";
        exchangeButton = exchangeButtonRoot.GetComponentInChildren<Button>(true);
        exchangeButtonText = exchangeButtonRoot.GetComponentInChildren<TMP_Text>(true);
        exchangeButtonRectTransform = exchangeButtonRoot.GetComponent<RectTransform>();
        exchangeButtonCanvasGroup = GetOrAddCanvasGroup(exchangeButtonRoot);
        SetActiveIfChanged(exchangeButtonRoot, false);
    }

    private void BindExchangeButtonEvent()
    {
        if (exchangeButton == null)
            return;

        exchangeButton.onClick.RemoveAllListeners();
        exchangeButton.onClick.AddListener(OnClickExchangeButton);
    }

    private void OnClickExchangeButton()
    {
        if (inspectedUnit != null && CanExchangeUnit(inspectedUnit.Data))
            TryExchangeInspectedUnit();
        else
            RefreshMergeUI();
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject targetRoot)
    {
        if (targetRoot == null)
            return null;

        CanvasGroup canvasGroup = targetRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = targetRoot.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private void SetButtonState(Button button, CanvasGroup canvasGroup, bool isInteractable)
    {
        SetInteractableIfChanged(button, isInteractable);

        if (canvasGroup == null)
            return;

        float targetAlpha = isInteractable ? enabledButtonAlpha : disabledButtonAlpha;
        if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            canvasGroup.alpha = targetAlpha;
        if (canvasGroup.interactable != isInteractable)
            canvasGroup.interactable = isInteractable;

        if (canvasGroup.blocksRaycasts != isInteractable)
            canvasGroup.blocksRaycasts = isInteractable;
    }

    private void RefreshMergeUI()
    {
        if (mergeButtonRoot == null)
            return;

        RefreshExchangeUI();

        if (inspectedUnit == null)
        {
            SetActiveIfChanged(mergeButtonRoot, false);
            return;
        }

        if (inspectedUnit.HasManualSelfEnhancement())
        {
            SetActiveIfChanged(mergeButtonRoot, true);
            RefreshManualEnhanceUI();
            return;
        }

        bool hasEvolutionRecipe = HasEvolutionRecipe(inspectedUnit) || IsRestrictedMergeUnit(inspectedUnit.Data);
        bool canEvolve = hasEvolutionRecipe && CanEvolveUnit(inspectedUnit);
        bool canMerge = !hasEvolutionRecipe && TryGetMergeInfo(inspectedUnit, out _, out _, out _);
        SetActiveIfChanged(mergeButtonRoot, true);

        if (mergeButtonText != null)
        {
            ApplyActionButtonFont(mergeButtonText);
            SetTextIfChanged(mergeButtonText, hasEvolutionRecipe ? evolveButtonLabel : mergeButtonLabel);
        }

        SetButtonState(mergeButton, mergeButtonCanvasGroup, canEvolve || canMerge);

        UpdateMergeButtonPosition();
    }

    private void RefreshExchangeUI()
    {
        if (exchangeButtonRoot == null)
            return;

        if (inspectedUnit == null || inspectedUnit.Data == null)
        {
            SetActiveIfChanged(exchangeButtonRoot, false);
            return;
        }

        bool canExchange = CanExchangeUnit(inspectedUnit.Data);
        int exchangeCost = GetUnitExchangeCost(inspectedUnit.Data);
        SetActiveIfChanged(exchangeButtonRoot, true);

        if (exchangeButtonText != null)
        {
            ApplyActionButtonFont(exchangeButtonText);
            SetTextIfChanged(
                exchangeButtonText,
                exchangeCost >= 0
                    ? $"{exchangeButtonLabel} {exchangeCost}"
                    : exchangeUnavailableLabel);
        }

        SetButtonState(exchangeButton, exchangeButtonCanvasGroup, canExchange);

        UpdateExchangeButtonPosition();
    }

    private void RefreshManualEnhanceUI()
    {
        if (inspectedUnit == null)
            return;

        SetButtonState(mergeButton, mergeButtonCanvasGroup, inspectedUnit.CanTryManualEnhance(goldManager));

        if (mergeButtonText != null)
        {
            ApplyActionButtonFont(mergeButtonText);
            int stack = inspectedUnit.GetManualEnhanceStack();
            int maxStack = inspectedUnit.GetManualEnhanceMaxStack();
            int cost = inspectedUnit.GetManualEnhanceCost();
            int chancePercent = Mathf.RoundToInt(inspectedUnit.GetManualEnhanceSuccessChance() * 100f);
            SetTextIfChanged(
                mergeButtonText,
                maxStack > 0 && stack >= maxStack
                    ? $"Enhance MAX ({stack}/{maxStack})"
                    : $"Enhance {cost}G {chancePercent}% ({stack}/{maxStack})");
        }

        UpdateMergeButtonPosition();
    }

    private void ApplyActionButtonFont(TMP_Text targetText)
    {
        if (targetText == null || actionButtonFontAsset == null)
            return;

        if (targetText.font == actionButtonFontAsset)
            return;

        targetText.font = actionButtonFontAsset;
    }

    private static void SetTextIfChanged(TMP_Text target, string value)
    {
        if (target == null || target.text == value)
            return;

        target.text = value;
    }

    private static void SetInteractableIfChanged(Button button, bool value)
    {
        if (button == null || button.interactable == value)
            return;

        button.interactable = value;
    }

    private static void SetActiveIfChanged(GameObject target, bool value)
    {
        if (target == null || target.activeSelf == value)
            return;

        target.SetActive(value);
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

    private void NotifyMissionFieldUnitsChanged()
    {
        MissionManager missionManager = MissionManager.Instance;
        if (missionManager == null)
            missionManager = FindAnyObjectByType<MissionManager>();

        if (missionManager != null)
            missionManager.NotifyFieldUnitsChanged();
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

    private void UpdateExchangeButtonPosition()
    {
        if (!positionMergeButtonNearSelectedUnit)
            return;

        if (exchangeButtonRoot == null || inspectedUnit == null)
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (exchangeButtonRectTransform == null)
            exchangeButtonRectTransform = exchangeButtonRoot.GetComponent<RectTransform>();

        if (exchangeButtonRectTransform == null)
            return;

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(inspectedUnit.transform.position);
        if (screenPosition.z <= 0f)
            return;

        exchangeButtonRectTransform.position = (Vector2)screenPosition + exchangeButtonScreenOffset;
    }

}
