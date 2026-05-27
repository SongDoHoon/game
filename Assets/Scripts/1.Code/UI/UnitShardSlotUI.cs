using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitShardSlotUI : MonoBehaviour
{
    [Header("Unit")]
    public Image unitImage;
    public TMP_Text unitNameText;
    public TMP_Text unitLevelText;

    [Header("Shard")]
    public Slider shardSlider;
    public TMP_Text shardCountText;
    public TMP_Text bonusText;
    public Button upgradeButton;

    [Header("Optional")]
    public TMP_Text upgradeCostText;
    public Image upgradeCostGoldIcon;
    public Sprite goldSprite;

    [Header("Display")]
    public bool hideCurrencyNameWhenIconIsAssigned = true;

    private UnitData unitData;
    private UnitGrowthManager unitGrowthManager;
    private UnitShardUpgradePanelUI ownerPanel;

    public void Initialize(UnitData data, UnitGrowthManager growthManager, UnitShardUpgradePanelUI panel)
    {
        unitData = data;
        unitGrowthManager = growthManager;
        ownerPanel = panel;

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(TryUpgrade);
            upgradeButton.onClick.AddListener(TryUpgrade);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (unitData == null || unitGrowthManager == null)
            return;

        UnitGrowthEntry growth = unitGrowthManager.GetUnitGrowth(unitData.unitId);
        int currentLevel = growth != null ? growth.unitShardLevel : 1;
        int ownedShardCount = growth != null ? growth.unitShardCount : 1;
        bool isMaxLevel = currentLevel >= UnitGrowthBalanceConfig.MaxUnitShardLevel;

        UnitShardUpgradeData currentData = UnitGrowthBalanceConfig.GetUnitShardData(currentLevel);
        UnitShardUpgradeData nextData = UnitGrowthBalanceConfig.GetUnitShardData(isMaxLevel ? currentLevel : currentLevel + 1);

        RefreshImage();

        SetTextIfChanged(unitNameText, !string.IsNullOrWhiteSpace(unitData.unitName) ? unitData.unitName : unitData.name);

        SetTextIfChanged(unitLevelText, $"조각 성장 Lv.{Mathf.Max(1, currentLevel)}");

        if (shardSlider != null)
        {
            SetSliderMaxValueIfChanged(shardSlider, isMaxLevel ? 1 : Mathf.Max(1, nextData.shardCost));
            SetSliderValueIfChanged(shardSlider, isMaxLevel ? shardSlider.maxValue : Mathf.Clamp(ownedShardCount, 0, nextData.shardCost));
        }

        SetTextIfChanged(shardCountText, isMaxLevel ? "MAX" : $"{ownedShardCount} / {nextData.shardCost}");

        SetTextIfChanged(
            bonusText,
            isMaxLevel
                ? $"현재: {FormatBonus(currentData)}\n다음: MAX"
                : $"현재: {FormatBonus(currentData)}\n다음: {FormatBonus(nextData)}");

        CurrencyDisplayUtility.SetIconSprite(upgradeCostGoldIcon, goldSprite);
        CurrencyDisplayUtility.SetIconVisible(upgradeCostGoldIcon, !isMaxLevel && CurrencyDisplayUtility.ShouldUseIcon(upgradeCostGoldIcon));

        SetTextIfChanged(upgradeCostText, isMaxLevel ? "MAX" : CurrencyDisplayUtility.FormatAmount("Gold", nextData.goldCost, upgradeCostGoldIcon, hideCurrencyNameWhenIconIsAssigned));

        SetInteractableIfChanged(upgradeButton, !isMaxLevel && unitGrowthManager.CanUpgradeUnitShard(unitData.unitId));
    }

    public void TryUpgrade()
    {
        if (unitData == null || unitGrowthManager == null)
            return;

        bool success = unitGrowthManager.TryUpgradeUnitShard(unitData.unitId);
        ownerPanel?.SetResultMessage(success ? "조각 강화 성공" : "조각 또는 골드가 부족합니다.");
        ownerPanel?.Refresh();
    }

    private void RefreshImage()
    {
        if (unitImage == null)
            return;

        Sprite sprite = unitData.portraitSprite != null ? unitData.portraitSprite : unitData.unitSprite;
        SetSpriteIfChanged(unitImage, sprite);
        SetActiveIfChanged(unitImage.gameObject, sprite != null);
    }

    private static string FormatBonus(UnitShardUpgradeData data)
    {
        int attackPercent = Mathf.RoundToInt(data.attackBonus * 100f);
        int attackSpeedPercent = Mathf.RoundToInt(data.attackSpeedBonus * 100f);
        return $"공격력 +{attackPercent}%, 공격속도 +{attackSpeedPercent}%";
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

    private static void SetSpriteIfChanged(Image image, Sprite sprite)
    {
        if (image == null || image.sprite == sprite)
            return;

        image.sprite = sprite;
    }

    private static void SetActiveIfChanged(GameObject target, bool value)
    {
        if (target == null || target.activeSelf == value)
            return;

        target.SetActive(value);
    }

    private static void SetSliderMaxValueIfChanged(Slider slider, float value)
    {
        if (slider == null || Mathf.Approximately(slider.maxValue, value))
            return;

        slider.maxValue = value;
    }

    private static void SetSliderValueIfChanged(Slider slider, float value)
    {
        if (slider == null || Mathf.Approximately(slider.value, value))
            return;

        slider.value = value;
    }
}
