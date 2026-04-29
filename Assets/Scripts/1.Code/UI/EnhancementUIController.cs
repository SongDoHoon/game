using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancementUIController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject enhancementPanel;
    public Button openButton;
    public Button closeButton;
    public bool closeOnStart = true;

    [Header("Gold")]
    public GoldManager goldManager;

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

    private void Awake()
    {
        if (goldManager == null)
            goldManager = FindFirstObjectByType<GoldManager>();

        BindButtonEvents();
        RefreshUI();

        if (closeOnStart)
            ClosePanel();
    }

    private void OnEnable()
    {
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
        RefreshGroupUI(UnitEnhanceGroup.LowGradeGroup, lowGradeLevelText, lowGradeCostText, lowGradeButton);
        RefreshGroupUI(UnitEnhanceGroup.HighGradeGroup, highGradeLevelText, highGradeCostText, highGradeButton);
        RefreshGroupUI(UnitEnhanceGroup.EvolutionGroup, evolutionLevelText, evolutionCostText, evolutionButton);
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
        if (goldManager == null)
        {
            SetResultText("GoldManager is not assigned.");
            return;
        }

        int beforeLevel = GameModifierState.GetEnhancementLevel(group);
        bool success = goldManager.TryEnhance(group);
        int afterLevel = GameModifierState.GetEnhancementLevel(group);

        if (success)
            SetResultText($"{displayName} enhanced to Lv.{afterLevel}.");
        else if (beforeLevel >= 10)
            SetResultText($"{displayName} is already max level.");
        else
            SetResultText($"Not enough gold for {displayName}.");

        RefreshUI();
    }

    private void RefreshGroupUI(UnitEnhanceGroup group, TMP_Text levelText, TMP_Text costText, Button button)
    {
        int level = GameModifierState.GetEnhancementLevel(group);
        bool isMaxLevel = level >= 10;
        int cost = isMaxLevel ? 0 : GameModifierState.GetNextEnhancementCost(group);

        if (levelText != null)
            levelText.text = $"Lv. {level}/10";

        if (costText != null)
            costText.text = isMaxLevel ? "MAX" : $"Cost: {cost}";

        if (button != null)
            button.interactable = goldManager != null && !isMaxLevel && goldManager.currentGold >= cost;
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }
}
