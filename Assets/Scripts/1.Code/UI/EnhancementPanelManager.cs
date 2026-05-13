using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancementPanelManager : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;

    [Header("References")]
    public BattleMagicStoneManager battleMagicStoneManager;

    [Header("Panel")]
    public GameObject enhancementPanel;
    public Button openButton;
    public Button closeButton;
    public bool closeOnStart = true;

    [Header("Magic Stone")]
    public TMP_Text currentMagicStoneText;

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

    [Header("Result")]
    public TMP_Text resultText;

    private float refreshTimer;

    private void Awake()
    {
        ResolveReferences();
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
        ResolveReferences();
        RefreshCurrentMagicStoneUI();

        RefreshGroupUI(UnitEnhanceGroup.LowGradeGroup, lowGradeLevelText, lowGradeCostText, lowGradeButton);
        RefreshGroupUI(UnitEnhanceGroup.HighGradeGroup, highGradeLevelText, highGradeCostText, highGradeButton);
        RefreshGroupUI(UnitEnhanceGroup.EvolutionGroup, evolutionLevelText, evolutionCostText, evolutionButton);
    }

    private void ResolveReferences()
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindFirstObjectByType<BattleMagicStoneManager>();
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
        ResolveReferences();

        if (battleMagicStoneManager == null)
        {
            SetResultText("인게임 마석 매니저가 배치되지 않았습니다.");
            return;
        }

        int beforeLevel = GameModifierState.GetEnhancementLevel(group);
        bool success = battleMagicStoneManager.TryUpgradeGradeGroup(group);
        int afterLevel = GameModifierState.GetEnhancementLevel(group);

        if (success)
            SetResultText($"{displayName} 강화 Lv.{afterLevel}");
        else if (beforeLevel >= GameBalanceConfig.MaxEnhancementLevel)
            SetResultText($"{displayName}은 이미 최대 강화입니다.");
        else
            SetResultText($"{displayName} 강화에 필요한 마석이 부족합니다.");

        RefreshUI();
    }

    private void RefreshGroupUI(UnitEnhanceGroup group, TMP_Text levelText, TMP_Text costText, Button button)
    {
        int level = GameModifierState.GetEnhancementLevel(group);
        bool isMaxLevel = level >= GameBalanceConfig.MaxEnhancementLevel;
        int cost = isMaxLevel ? 0 : GameModifierState.GetNextEnhancementCost(group);

        if (levelText != null)
            levelText.text = $"Lv. {level}/{GameBalanceConfig.MaxEnhancementLevel}";

        if (costText != null)
            costText.text = isMaxLevel ? "MAX" : $"마석 {cost}";

        if (button != null)
            button.interactable = battleMagicStoneManager != null && !isMaxLevel && battleMagicStoneManager.CurrentBattleMagicStone >= cost;
    }

    private void RefreshCurrentMagicStoneUI()
    {
        if (currentMagicStoneText != null)
        {
            double currentMagicStone = battleMagicStoneManager != null ? battleMagicStoneManager.CurrentBattleMagicStone : 0.0;
            currentMagicStoneText.text = $"마석 {System.Math.Floor(currentMagicStone)}";
        }
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }
}
