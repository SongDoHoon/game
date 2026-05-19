using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancementUIController : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;

    [Header("Panel")]
    public GameObject enhancementPanel;
    public Button openButton;
    public Button closeButton;
    public bool closeOnStart = true;

    [Header("Magic Stone")]
    public BattleMagicStoneManager battleMagicStoneManager;
    public TMP_Text currentMagicStoneText;
    public Image currentMagicStoneIcon;
    public Sprite magicStoneSprite;

    [Header("Low Grade Group")]
    public Button lowGradeButton;
    public TMP_Text lowGradeLevelText;
    public TMP_Text lowGradeCostText;
    public Image lowGradeCostMagicStoneIcon;

    [Header("High Grade Group")]
    public Button highGradeButton;
    public TMP_Text highGradeLevelText;
    public TMP_Text highGradeCostText;
    public Image highGradeCostMagicStoneIcon;

    [Header("Evolution Group")]
    public Button evolutionButton;
    public TMP_Text evolutionLevelText;
    public TMP_Text evolutionCostText;
    public Image evolutionCostMagicStoneIcon;

    [Header("Result")]
    public TMP_Text resultText;

    [Header("Display")]
    public bool hideCurrencyNameWhenIconIsAssigned = true;
    public bool createIconWhenSpriteIsAssigned = true;
    public Vector2 iconSize = new(32f, 32f);
    public float iconSpacing = 6f;

    private float refreshTimer;

    private void Awake()
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        BindButtonEvents();
        RefreshUI();

        if (closeOnStart)
            ClosePanel();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;
        RefreshUI();
    }

    public void OpenPanel()
    {
        if (enhancementPanel != null)
            enhancementPanel.SetActive(true);

        RefreshUI();
    }

    public void ClosePanel()
    {
        if (enhancementPanel != null)
            enhancementPanel.SetActive(false);
    }

    public void EnhanceLowGrade()
    {
        TryEnhance(UnitEnhanceGroup.LowGradeGroup, "Low Grade");
    }

    public void EnhanceHighGrade()
    {
        TryEnhance(UnitEnhanceGroup.HighGradeGroup, "High Grade");
    }

    public void EnhanceEvolution()
    {
        TryEnhance(UnitEnhanceGroup.EvolutionGroup, "Evolution");
    }

    public void RefreshUI()
    {
        RefreshCurrentMagicStoneUI();
        EnsureCostIcons();
        RefreshGroupUI(UnitEnhanceGroup.LowGradeGroup, lowGradeLevelText, lowGradeCostText, lowGradeButton, lowGradeCostMagicStoneIcon);
        RefreshGroupUI(UnitEnhanceGroup.HighGradeGroup, highGradeLevelText, highGradeCostText, highGradeButton, highGradeCostMagicStoneIcon);
        RefreshGroupUI(UnitEnhanceGroup.EvolutionGroup, evolutionLevelText, evolutionCostText, evolutionButton, evolutionCostMagicStoneIcon);
    }

    private void BindButtonEvents()
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

        if (lowGradeButton != null)
        {
            lowGradeButton.onClick.RemoveListener(EnhanceLowGrade);
            lowGradeButton.onClick.AddListener(EnhanceLowGrade);
        }

        if (highGradeButton != null)
        {
            highGradeButton.onClick.RemoveListener(EnhanceHighGrade);
            highGradeButton.onClick.AddListener(EnhanceHighGrade);
        }

        if (evolutionButton != null)
        {
            evolutionButton.onClick.RemoveListener(EnhanceEvolution);
            evolutionButton.onClick.AddListener(EnhanceEvolution);
        }
    }

    private void TryEnhance(UnitEnhanceGroup group, string displayName)
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        if (battleMagicStoneManager == null)
        {
            SetResultText("?∏Í≤å??ÎßàÏÑù Îß§Îãà?ÄÍ∞Ä Î∞∞Ïπò?òÏ? ?äÏïò?µÎãà??");
            return;
        }

        int beforeLevel = GameModifierState.GetEnhancementLevel(group);
        bool success = battleMagicStoneManager.TryUpgradeGradeGroup(group);
        int afterLevel = GameModifierState.GetEnhancementLevel(group);

        if (success)
            SetResultText($"{displayName} Í∞ïÌôî Lv.{afterLevel}");
        else if (beforeLevel >= GameBalanceConfig.MaxEnhancementLevel)
            SetResultText($"{displayName}?Ä ?¥Î? ÏµúÎ? Í∞ïÌôî?ÖÎãà??");
        else
            SetResultText($"{displayName} Í∞ïÌôî???ÑÏöî??ÎßàÏÑù??Î∂ÄÏ°±Ìï©?àÎã§.");

        RefreshUI();
    }

    private void RefreshGroupUI(UnitEnhanceGroup group, TMP_Text levelText, TMP_Text costText, Button button, Image costIcon)
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        int level = GameModifierState.GetEnhancementLevel(group);
        bool isMaxLevel = level >= GameBalanceConfig.MaxEnhancementLevel;
        int cost = isMaxLevel ? 0 : GameModifierState.GetNextEnhancementCost(group);

        if (levelText != null)
            levelText.text = $"Lv. {level}/{GameBalanceConfig.MaxEnhancementLevel}";

        SetIconSprite(costIcon, magicStoneSprite);
        SetIconVisible(costIcon, !isMaxLevel && ShouldUseIcon(costIcon));

        if (costText != null)
            costText.text = isMaxLevel ? "MAX" : FormatCurrencyAmount("Magic Stone", cost, costIcon);

        if (button != null)
            button.interactable = battleMagicStoneManager != null && !isMaxLevel && battleMagicStoneManager.CurrentBattleMagicStone >= cost;
    }

    private void RefreshCurrentMagicStoneUI()
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        if (currentMagicStoneText != null)
        {
            double currentMagicStone = battleMagicStoneManager != null ? battleMagicStoneManager.CurrentBattleMagicStone : 0.0;
            currentMagicStoneIcon = EnsureIconImage(currentMagicStoneIcon, currentMagicStoneText, "Current Magic Stone Icon", magicStoneSprite);
            SetIconSprite(currentMagicStoneIcon, magicStoneSprite);
            currentMagicStoneText.text = FormatCurrencyAmount("Magic Stone", System.Math.Floor(currentMagicStone), currentMagicStoneIcon);
        }

        SetIconVisible(currentMagicStoneIcon, ShouldUseIcon(currentMagicStoneIcon));
    }

    private void EnsureCostIcons()
    {
        lowGradeCostMagicStoneIcon = EnsureIconImage(lowGradeCostMagicStoneIcon, lowGradeCostText, "Low Grade Magic Stone Cost Icon", magicStoneSprite);
        highGradeCostMagicStoneIcon = EnsureIconImage(highGradeCostMagicStoneIcon, highGradeCostText, "High Grade Magic Stone Cost Icon", magicStoneSprite);
        evolutionCostMagicStoneIcon = EnsureIconImage(evolutionCostMagicStoneIcon, evolutionCostText, "Evolution Magic Stone Cost Icon", magicStoneSprite);
    }
    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

    private string FormatCurrencyAmount(string currencyName, double amount, Image icon)
    {
        bool useIcon = ShouldUseIcon(icon);
        string amountText = amount.ToString("0");
        return useIcon && hideCurrencyNameWhenIconIsAssigned ? amountText : $"{currencyName} {amountText}";
    }

    private static bool ShouldUseIcon(Image icon)
    {
        return icon != null && icon.sprite != null;
    }

    private static void SetIconSprite(Image icon, Sprite sprite)
    {
        if (icon != null && sprite != null)
            icon.sprite = sprite;
    }

    private static void SetIconVisible(Image icon, bool visible)
    {
        if (icon != null)
            icon.gameObject.SetActive(visible);
    }

    private Image EnsureIconImage(Image icon, TMP_Text targetText, string iconObjectName, Sprite sprite)
    {
        if (icon != null || sprite == null || !createIconWhenSpriteIsAssigned || targetText == null)
            return icon;

        GameObject iconObject = new GameObject(iconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(targetText.transform, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = new Vector2(-iconSpacing, 0f);

        Image createdIcon = iconObject.GetComponent<Image>();
        createdIcon.raycastTarget = false;
        return createdIcon;
    }}
