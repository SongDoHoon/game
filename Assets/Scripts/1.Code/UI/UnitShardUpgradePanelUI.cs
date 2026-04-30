using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitShardUpgradePanelUI : MonoBehaviour
{
    [Header("References")]
    public UnitGrowthManager unitGrowthManager;
    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;
    public Transform slotParent;
    public UnitShardSlotUI slotPrefab;

    [Header("Texts")]
    public TMP_Text mainGoldText;
    public TMP_Text resultText;

    [Header("Grid Layout")]
    public bool configureSlotGridLayout = true;
    public Vector2 slotCellSize = new(150f, 150f);
    public Vector2 slotSpacing = new(8f, 8f);
    public int fixedColumnCount = 0;

    private readonly List<UnitShardSlotUI> slots = new();

    private void Awake()
    {
        if (unitGrowthManager == null)
            unitGrowthManager = UnitGrowthManager.Instance;

        BindButtons();
        ConfigureSlotGridLayout();
        BuildSlots();
        Refresh();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        BuildSlots();
        Refresh();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (unitGrowthManager == null)
            unitGrowthManager = UnitGrowthManager.Instance;

        foreach (UnitShardSlotUI slot in slots)
        {
            if (slot != null)
                slot.Refresh();
        }

        if (mainGoldText != null && unitGrowthManager != null)
            mainGoldText.text = $"Gold {unitGrowthManager.GetAvailableGrowthGold()}";
    }

    public void SetResultMessage(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

    private void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void BuildSlots()
    {
        if (unitGrowthManager == null || slotParent == null || slotPrefab == null)
            return;

        if (slots.Count > 0)
            return;

        foreach (UnitData unitData in unitGrowthManager.unitDatabase)
        {
            if (unitData == null || string.IsNullOrWhiteSpace(unitData.unitId))
                continue;

            UnitShardSlotUI slot = Instantiate(slotPrefab, slotParent);
            slot.Initialize(unitData, unitGrowthManager, this);
            slots.Add(slot);
        }
    }

    private void ConfigureSlotGridLayout()
    {
        if (!configureSlotGridLayout || slotParent == null)
            return;

        RectTransform slotParentRect = slotParent as RectTransform;
        if (slotParentRect != null)
        {
            slotParentRect.pivot = new Vector2(0f, 1f);
            slotParentRect.anchorMin = new Vector2(0f, 1f);
            slotParentRect.anchorMax = new Vector2(1f, 1f);
        }

        GridLayoutGroup gridLayoutGroup = slotParent.GetComponent<GridLayoutGroup>();
        RemoveConflictingLayoutGroups(gridLayoutGroup);

        if (gridLayoutGroup == null)
            gridLayoutGroup = slotParent.gameObject.AddComponent<GridLayoutGroup>();

        gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        gridLayoutGroup.cellSize = slotCellSize;
        gridLayoutGroup.spacing = slotSpacing;
        gridLayoutGroup.constraint = fixedColumnCount > 0 ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.Flexible;
        gridLayoutGroup.constraintCount = Mathf.Max(1, fixedColumnCount);

        ContentSizeFitter contentSizeFitter = slotParent.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
            contentSizeFitter = slotParent.gameObject.AddComponent<ContentSizeFitter>();

        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void RemoveConflictingLayoutGroups(GridLayoutGroup gridLayoutGroup)
    {
        LayoutGroup[] layoutGroups = slotParent.GetComponents<LayoutGroup>();

        foreach (LayoutGroup layoutGroup in layoutGroups)
        {
            if (layoutGroup == null || layoutGroup == gridLayoutGroup)
                continue;

            DestroyImmediate(layoutGroup);
        }
    }
}
