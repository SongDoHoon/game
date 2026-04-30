using System.Reflection;
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

        if (unitNameText != null)
            unitNameText.text = !string.IsNullOrWhiteSpace(unitData.unitName) ? unitData.unitName : unitData.name;

        if (unitLevelText != null)
            unitLevelText.text = $"조각 성장 Lv.{Mathf.Max(1, currentLevel)}";

        if (shardSlider != null)
        {
            shardSlider.maxValue = isMaxLevel ? 1 : Mathf.Max(1, nextData.shardCost);
            shardSlider.value = isMaxLevel ? shardSlider.maxValue : Mathf.Clamp(ownedShardCount, 0, nextData.shardCost);
        }

        if (shardCountText != null)
            shardCountText.text = isMaxLevel ? "MAX" : $"{ownedShardCount} / {nextData.shardCost}";

        if (bonusText != null)
            bonusText.text = isMaxLevel
                ? $"현재: {FormatBonus(currentData)}\n다음: MAX"
                : $"현재: {FormatBonus(currentData)}\n다음: {FormatBonus(nextData)}";

        if (upgradeCostText != null)
            upgradeCostText.text = isMaxLevel ? "MAX" : $"골드 {nextData.goldCost}";

        if (upgradeButton != null)
            upgradeButton.interactable = !isMaxLevel && unitGrowthManager.CanUpgradeUnitShard(unitData.unitId);
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

        Sprite sprite = FindUnitSprite(unitData);
        unitImage.sprite = sprite;
        unitImage.gameObject.SetActive(sprite != null);
    }

    private static Sprite FindUnitSprite(UnitData data)
    {
        if (data == null)
            return null;

        FieldInfo[] fields = data.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(Sprite))
                return field.GetValue(data) as Sprite;
        }

        PropertyInfo[] properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (PropertyInfo property in properties)
        {
            if (property.CanRead && property.PropertyType == typeof(Sprite) && property.GetIndexParameters().Length == 0)
                return property.GetValue(data) as Sprite;
        }

        return null;
    }

    private static string FormatBonus(UnitShardUpgradeData data)
    {
        int attackPercent = Mathf.RoundToInt(data.attackBonus * 100f);
        int attackSpeedPercent = Mathf.RoundToInt(data.attackSpeedBonus * 100f);
        return $"공격력 +{attackPercent}%, 공격속도 +{attackSpeedPercent}%";
    }
}
