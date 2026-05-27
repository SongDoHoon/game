using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EvolutionMaterialGroup
{
    DivineBeast,
    FourFiend
}

[System.Serializable]
public class EvolutionMaterialDisplayData
{
    private const string DefaultAcquisitionText = "\uD68D\uB4DD\uCC98 : \uBCF4\uC2A4 \uCC98\uCE58 \uD6C4 \uACBD\uB9E4";

    public EvolutionItemType itemType;
    public EvolutionMaterialGroup group;
    public string displayName;
    public Sprite backgroundSprite;
    public Sprite itemSprite;
    public string acquisitionText = DefaultAcquisitionText;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? itemType.ToString() : displayName;
}

public class EvolutionMaterialPanelManager : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;

    [Header("Root")]
    public GameObject panelRoot;

    [Header("Buttons")]
    public Button openButton;
    public Button closeButton;

    [Header("Containers")]
    public Transform divineBeastContentRoot;
    public Transform fourFiendContentRoot;

    [Header("Prefab")]
    public EvolutionMaterialSlotUI materialSlotPrefab;

    [Header("Managers")]
    public EvolutionItemInventory itemInventory;
    public EvolutionManager evolutionManager;

    [Header("Display Data")]
    public List<EvolutionMaterialDisplayData> materialDisplayData = new();

    private readonly Dictionary<EvolutionItemType, EvolutionMaterialSlotUI> slotsByItem = new();
    private float refreshTimer;

    private void Awake()
    {
        BindButtonEvents();
        FindManagersIfNeeded();
        EnsureDefaultDisplayData();
        BuildSlots();
        ClosePanel();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDefaultDisplayData();
    }
#endif

    private void OnEnable()
    {
        if (itemInventory != null)
            itemInventory.OnItemCountChanged += HandleItemCountChanged;
    }

    private void OnDisable()
    {
        if (itemInventory != null)
            itemInventory.OnItemCountChanged -= HandleItemCountChanged;
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;
        RefreshAllSlots();
    }

    public void OpenPanel()
    {
        InGamePanelCoordinator.CloseOtherPanels(panelRoot);
        refreshTimer = 0f;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshAllSlots();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
            return;

        if (panelRoot.activeSelf)
            ClosePanel();
        else
            OpenPanel();
    }

    public void RefreshAllSlots()
    {
        FindManagersIfNeeded();

        foreach (EvolutionMaterialDisplayData displayData in materialDisplayData)
            RefreshSlot(displayData);
    }

    private void BindButtonEvents()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.RemoveListener(TogglePanel);
            openButton.onClick.AddListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void FindManagersIfNeeded()
    {
        if (itemInventory == null)
            itemInventory = FindAnyObjectByType<EvolutionItemInventory>();

        if (evolutionManager == null)
            evolutionManager = FindAnyObjectByType<EvolutionManager>();
    }

    private void BuildSlots()
    {
        if (materialSlotPrefab == null)
            return;

        slotsByItem.Clear();

        foreach (EvolutionMaterialDisplayData displayData in materialDisplayData)
        {
            if (displayData == null || displayData.itemType == EvolutionItemType.None)
                continue;

            Transform parent = displayData.group == EvolutionMaterialGroup.DivineBeast
                ? divineBeastContentRoot
                : fourFiendContentRoot;

            if (parent == null)
                continue;

            EvolutionMaterialSlotUI slot = Instantiate(materialSlotPrefab, parent);
            slotsByItem[displayData.itemType] = slot;
            RefreshSlot(displayData);
        }
    }

    private void RefreshSlot(EvolutionMaterialDisplayData displayData)
    {
        if (displayData == null || displayData.itemType == EvolutionItemType.None)
            return;

        if (!slotsByItem.TryGetValue(displayData.itemType, out EvolutionMaterialSlotUI slot) || slot == null)
            return;

        int ownedCount = itemInventory != null ? itemInventory.GetCount(displayData.itemType) : 0;
        List<EvolutionRecipe> recipes = GetRecipes(displayData.itemType);
        slot.Refresh(displayData, ownedCount, recipes);
    }

    private List<EvolutionRecipe> GetRecipes(EvolutionItemType itemType)
    {
        List<EvolutionRecipe> recipes = new();

        if (evolutionManager == null || evolutionManager.recipes == null)
            return recipes;

        foreach (EvolutionRecipe recipe in evolutionManager.recipes)
        {
            if (recipe == null || recipe.requiredItem != itemType)
                continue;

            recipes.Add(recipe);
        }

        return recipes;
    }

    private void HandleItemCountChanged(EvolutionItemType itemType, int count)
    {
        EvolutionMaterialDisplayData displayData = materialDisplayData.Find(data => data != null && data.itemType == itemType);
        RefreshSlot(displayData);
    }

    private void EnsureDefaultDisplayData()
    {
        AddDefaultDisplayData(EvolutionItemType.Baekho, EvolutionMaterialGroup.DivineBeast, "\uBC31\uD638");
        AddDefaultDisplayData(EvolutionItemType.Cheongryong, EvolutionMaterialGroup.DivineBeast, "\uCCAD\uB8E1");
        AddDefaultDisplayData(EvolutionItemType.Hyeonmu, EvolutionMaterialGroup.DivineBeast, "\uD604\uBB34");
        AddDefaultDisplayData(EvolutionItemType.Jujak, EvolutionMaterialGroup.DivineBeast, "\uC8FC\uC791");
        AddDefaultDisplayData(EvolutionItemType.Taotie, EvolutionMaterialGroup.FourFiend, "\uB3C4\uCCA0");
        AddDefaultDisplayData(EvolutionItemType.Qiongqi, EvolutionMaterialGroup.FourFiend, "\uAD81\uAE30");
        AddDefaultDisplayData(EvolutionItemType.Taowu, EvolutionMaterialGroup.FourFiend, "\uB3C4\uC62C");
        AddDefaultDisplayData(EvolutionItemType.Hundun, EvolutionMaterialGroup.FourFiend, "\uD63C\uB3C8");
    }

    private void AddDefaultDisplayData(EvolutionItemType itemType, EvolutionMaterialGroup group, string displayName)
    {
        if (materialDisplayData.Exists(data => data != null && data.itemType == itemType))
            return;

        materialDisplayData.Add(new EvolutionMaterialDisplayData
        {
            itemType = itemType,
            group = group,
            displayName = displayName
        });
    }
}
