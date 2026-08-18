using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancementUIController : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;

    [System.Serializable]
    private sealed class EnhancementGroupImageUI
    {
        public Image groupPanelImage;
        public Sprite groupPanelSprite;
        public Image enhanceButtonImage;
        public Sprite enhanceButtonSprite;
        public Image gradeTextBackgroundImage;
        public Sprite gradeTextBackgroundSprite;
        public TMP_Text gradeText;
        public string gradeTextValue;
    }

    [Header("Panel")]
    public GameObject enhancementPanel;
    public Button openButton;
    public Button closeButton;
    public bool closeOnStart = true;

    [Header("Image Based Panel")]
    [SerializeField] private Image enhancementPanelImage;
    [SerializeField] private Sprite enhancementPanelSprite;

    [Header("Image Based Grade Groups")]
    [SerializeField] private EnhancementGroupImageUI lowGradeImageUI = new()
    {
        gradeTextValue = "노멀 ~ 레어"
    };
    [SerializeField] private EnhancementGroupImageUI highGradeImageUI = new()
    {
        gradeTextValue = "에픽 ~ 베르어"
    };
    [SerializeField] private EnhancementGroupImageUI evolutionImageUI = new()
    {
        gradeTextValue = "대천사 ~ 대악마"
    };

    [Header("Magic Stone")]
    public BattleMagicStoneManager battleMagicStoneManager;
    public TMP_Text currentMagicStoneText;
    public Sprite magicStoneSprite;

    [Header("Low Grade Group")]
    public Button lowGradeButton;
    public TMP_Text lowGradeLevelText;
    public TMP_Text lowGradeCostText;

    [Header("High Grade Group")]
    public Button highGradeButton;
    public TMP_Text highGradeLevelText;
    public TMP_Text highGradeCostText;

    [Header("Evolution Group")]
    public Button evolutionButton;
    public TMP_Text evolutionLevelText;
    public TMP_Text evolutionCostText;


    [Header("Display")]
    public bool hideCurrencyNameWhenIconIsAssigned = true;
    public bool createIconWhenSpriteIsAssigned = true;
    public Vector2 iconSize = new(32f, 32f);
    public float iconSpacing = 6f;

    private float refreshTimer;
    private Image currentMagicStoneIcon;
    private Image lowGradeCostMagicStoneIcon;
    private Image highGradeCostMagicStoneIcon;
    private Image evolutionCostMagicStoneIcon;
    private string lastCurrentMagicStoneText;

    private void Awake()
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        ApplyImageLayout();
        BindButtonEvents();
        RefreshUI();

        if (closeOnStart)
            ClosePanel();
    }

    private void OnEnable()
    {
        ApplyImageLayout();
        RefreshUI();
    }

    private void OnValidate()
    {
        ApplyImageLayout();
    }

    private void Update()
    {
        if (enhancementPanel != null && !enhancementPanel.activeInHierarchy)
            return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;
        RefreshUI();
    }

    public void OpenPanel()
    {
        InGamePanelCoordinator.CloseOtherPanels(enhancementPanel);

        if (enhancementPanel != null)
            enhancementPanel.SetActive(true);

        RefreshUI();
    }

    public void ClosePanel()
    {
        if (enhancementPanel != null)
            enhancementPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (enhancementPanel == null)
            return;

        if (enhancementPanel.activeSelf)
            ClosePanel();
        else
            OpenPanel();
    }

    public void EnhanceLowGrade()
    {
        TryEnhance(UnitEnhanceGroup.LowGradeGroup);
    }

    public void EnhanceHighGrade()
    {
        TryEnhance(UnitEnhanceGroup.HighGradeGroup);
    }

    public void EnhanceEvolution()
    {
        TryEnhance(UnitEnhanceGroup.EvolutionGroup);
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
            openButton.onClick.RemoveListener(TogglePanel);
            openButton.onClick.AddListener(TogglePanel);
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

    private void TryEnhance(UnitEnhanceGroup group)
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        if (battleMagicStoneManager == null)
            return;

        battleMagicStoneManager.TryUpgradeGradeGroup(group);
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
            SetTextIfChanged(levelText, $"Lv. {level}/{GameBalanceConfig.MaxEnhancementLevel}");

        CurrencyDisplayUtility.SetIconSprite(costIcon, magicStoneSprite);
        CurrencyDisplayUtility.SetIconVisible(costIcon, !isMaxLevel && CurrencyDisplayUtility.ShouldUseIcon(costIcon));

        if (costText != null)
            SetTextIfChanged(costText, isMaxLevel ? "MAX" : CurrencyDisplayUtility.FormatAmount("Magic Stone", cost, costIcon, hideCurrencyNameWhenIconIsAssigned));

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
            currentMagicStoneIcon = CurrencyDisplayUtility.EnsureIconImage(currentMagicStoneIcon, currentMagicStoneText, "Current Magic Stone Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
            CurrencyDisplayUtility.SetIconSprite(currentMagicStoneIcon, magicStoneSprite);
            SetTextIfChanged(currentMagicStoneText, ref lastCurrentMagicStoneText, CurrencyDisplayUtility.FormatAmount("Magic Stone", System.Math.Floor(currentMagicStone), currentMagicStoneIcon, hideCurrencyNameWhenIconIsAssigned));
        }

        CurrencyDisplayUtility.SetIconVisible(currentMagicStoneIcon, CurrencyDisplayUtility.ShouldUseIcon(currentMagicStoneIcon));
    }

    private void EnsureCostIcons()
    {
        lowGradeCostMagicStoneIcon = CurrencyDisplayUtility.EnsureIconImage(lowGradeCostMagicStoneIcon, lowGradeCostText, "Low Grade Magic Stone Cost Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        highGradeCostMagicStoneIcon = CurrencyDisplayUtility.EnsureIconImage(highGradeCostMagicStoneIcon, highGradeCostText, "High Grade Magic Stone Cost Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        evolutionCostMagicStoneIcon = CurrencyDisplayUtility.EnsureIconImage(evolutionCostMagicStoneIcon, evolutionCostText, "Evolution Magic Stone Cost Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
    }

    private void ApplyImageLayout()
    {
        if (enhancementPanelImage == null && enhancementPanel != null)
            enhancementPanelImage = enhancementPanel.GetComponent<Image>();

        ApplySprite(enhancementPanelImage, enhancementPanelSprite);
        ApplyGroupImageUI(lowGradeImageUI, lowGradeButton);
        ApplyGroupImageUI(highGradeImageUI, highGradeButton);
        ApplyGroupImageUI(evolutionImageUI, evolutionButton);
    }

    private static void ApplyGroupImageUI(EnhancementGroupImageUI imageUI, Button enhanceButton)
    {
        if (imageUI == null)
            return;

        ApplySprite(imageUI.groupPanelImage, imageUI.groupPanelSprite);
        ApplySprite(
            imageUI.enhanceButtonImage != null ? imageUI.enhanceButtonImage : enhanceButton != null ? enhanceButton.image : null,
            imageUI.enhanceButtonSprite);
        ApplySprite(imageUI.gradeTextBackgroundImage, imageUI.gradeTextBackgroundSprite);

        if (imageUI.gradeText != null)
            SetTextIfChanged(imageUI.gradeText, imageUI.gradeTextValue);
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null || image.sprite == sprite)
            return;

        image.sprite = sprite;
    }

    private static bool SetTextIfChanged(TMP_Text text, string value)
    {
        if (text == null || text.text == value)
            return false;

        text.text = value;
        return true;
    }

    private static bool SetTextIfChanged(TMP_Text text, ref string cachedValue, string value)
    {
        if (cachedValue == value)
            return false;

        cachedValue = value;
        return SetTextIfChanged(text, value);
    }
}
