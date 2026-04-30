using UnityEngine;

public class UnitGrowthManager : MonoBehaviour
{
    public static UnitGrowthManager Instance { get; private set; }

    [Header("Data")]
    public UnitData[] unitDatabase;
    public UnitGrowthSaveData unitGrowthSaveData = new();
    public PlayerPassiveGrowthData playerPassiveGrowthData = new();

    [Header("Gold")]
    public GoldManager goldManager;
    public bool useMainGoldForGrowthCost;

    [Header("Save")]
    public bool loadSavedDataOnAwake = true;
    public bool saveDataOnChange = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (goldManager == null)
            goldManager = FindFirstObjectByType<GoldManager>();

        if (loadSavedDataOnAwake)
            LoadGrowthData();

        EnsureDefaultUnitGrowthData();
        RefreshPlayerPassiveTotals();
    }

    public bool TryUpgradeUnitShard(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetOrCreateUnit(unitId);
        if (entry.unitShardLevel >= UnitGrowthBalanceConfig.MaxUnitShardLevel)
            return false;

        UnitShardUpgradeData nextData = UnitGrowthBalanceConfig.GetUnitShardData(entry.unitShardLevel + 1);
        if (!CanPayGold(nextData.goldCost) || entry.unitShardCount < nextData.shardCost)
            return false;

        if (!SpendGrowthGold(nextData.goldCost))
            return false;

        entry.unitShardCount -= nextData.shardCost;
        entry.unitShardLevel++;
        OnGrowthDataChanged(true);
        return true;
    }

    public bool TryUpgradeMagicStone(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetOrCreateUnit(unitId);
        if (entry.magicStoneLevel >= UnitGrowthBalanceConfig.MaxMagicStoneLevel)
            return false;

        MagicStoneUpgradeData nextData = UnitGrowthBalanceConfig.GetMagicStoneData(entry.magicStoneLevel + 1);
        if (!CanPayGold(nextData.goldCost) || entry.magicStoneCount < nextData.stoneCost)
            return false;

        if (!SpendGrowthGold(nextData.goldCost))
            return false;

        entry.magicStoneCount -= nextData.stoneCost;
        entry.magicStoneLevel++;
        OnGrowthDataChanged(true);
        return true;
    }

    public bool TryUpgradeEquipment(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetOrCreateUnit(unitId);
        if (entry.equipmentLevel >= UnitGrowthBalanceConfig.MaxEquipmentLevel)
            return false;

        EquipmentUpgradeData nextData = UnitGrowthBalanceConfig.GetEquipmentData(entry.equipmentLevel + 1);
        if (!CanPayGold(nextData.goldCost))
            return false;

        if (!SpendGrowthGold(nextData.goldCost))
            return false;

        entry.equipmentLevel++;
        OnGrowthDataChanged(true);
        return true;
    }

    public bool TryUpgradeRelic(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetOrCreateUnit(unitId);
        if (entry.relicLevel >= UnitGrowthBalanceConfig.MaxRelicLevel)
            return false;

        RelicUpgradeData nextData = UnitGrowthBalanceConfig.GetRelicData(entry.relicLevel + 1);
        if (!CanPayGold(nextData.goldCost) || entry.relicCount < nextData.relicCost)
            return false;

        if (!SpendGrowthGold(nextData.goldCost))
            return false;

        entry.relicCount -= nextData.relicCost;
        entry.relicLevel++;
        OnGrowthDataChanged(true);
        return true;
    }

    public bool TryUpgradeSkillWithWishStone(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetOrCreateUnit(unitId);
        if (entry.skillLevel >= UnitGrowthBalanceConfig.MaxWishStoneSkillLevel)
            return false;

        WishStoneSkillUpgradeData nextData = UnitGrowthBalanceConfig.GetWishStoneSkillData(entry.skillLevel + 1);
        if (!CanPayGold(nextData.goldCost) || entry.wishStoneCount < nextData.wishStoneCost)
            return false;

        if (!SpendGrowthGold(nextData.goldCost))
            return false;

        entry.wishStoneCount -= nextData.wishStoneCost;
        entry.skillLevel++;
        OnGrowthDataChanged(false);
        return true;
    }

    public bool TryUpgradePlayerPassive(string passiveId)
    {
        PlayerPassiveGrowthEntry entry = playerPassiveGrowthData.GetOrCreatePassive(passiveId);
        if (entry.level >= UnitGrowthBalanceConfig.MaxPlayerPassiveLevel)
            return false;

        PlayerPassiveUpgradeData nextData = UnitGrowthBalanceConfig.GetPlayerPassiveData(entry.level + 1);
        if (!CanPayGold(nextData.goldCost) || playerPassiveGrowthData.enhancementStoneCount < nextData.stoneCost)
            return false;

        if (!SpendGrowthGold(nextData.goldCost))
            return false;

        playerPassiveGrowthData.enhancementStoneCount -= nextData.stoneCost;
        entry.level++;
        OnGrowthDataChanged(true);
        return true;
    }

    public double GetFinalAttack(string unitId)
    {
        UnitData unitData = FindUnitData(unitId);
        return GetStatResult(unitData).finalAttack;
    }

    public float GetFinalAttackInterval(string unitId)
    {
        UnitData unitData = FindUnitData(unitId);
        return GetStatResult(unitData).finalAttackInterval;
    }

    public float GetSkillDamageMultiplier(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetUnit(unitId);
        return UnitStatCalculator.GetSkillDamageMultiplier(entry);
    }

    public float GetSkillCooldownReduction(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetUnit(unitId);
        return UnitStatCalculator.GetSkillCooldownReduction(entry);
    }

    public UnitStatModifierResult GetStatResult(UnitData unitData)
    {
        if (unitData == null)
            return default;

        UnitGrowthEntry entry = unitGrowthSaveData.GetUnit(unitData.unitId);
        return UnitStatCalculator.Calculate(
            unitData,
            entry,
            playerPassiveGrowthData,
            GameModifierState.GlobalAttackPowerBonus,
            GameModifierState.GlobalAttackSpeedBonus);
    }

    public UnitGrowthEntry GetUnitGrowth(string unitId)
    {
        return unitGrowthSaveData.GetUnit(unitId);
    }

    public bool CanUpgradeUnitShard(string unitId)
    {
        UnitGrowthEntry entry = unitGrowthSaveData.GetUnit(unitId);
        if (entry == null || entry.unitShardLevel >= UnitGrowthBalanceConfig.MaxUnitShardLevel)
            return false;

        UnitShardUpgradeData nextData = UnitGrowthBalanceConfig.GetUnitShardData(entry.unitShardLevel + 1);
        return entry.unitShardCount >= nextData.shardCost && CanPayGold(nextData.goldCost);
    }

    public int GetAvailableGrowthGold()
    {
        if (useMainGoldForGrowthCost)
        {
            if (PlayerProgressManager.Instance != null)
                return PlayerProgressManager.Instance.playerProgressData.mainGold;

            return PlayerProgressSaveSystem.Data.mainGold;
        }

        return goldManager != null ? goldManager.currentGold : 0;
    }

    public void AddUnitShard(string unitId, int amount)
    {
        unitGrowthSaveData.GetOrCreateUnit(unitId).unitShardCount += Mathf.Max(0, amount);
        OnGrowthDataChanged(false);
    }

    public void AddMagicStone(string unitId, int amount)
    {
        unitGrowthSaveData.GetOrCreateUnit(unitId).magicStoneCount += Mathf.Max(0, amount);
        OnGrowthDataChanged(false);
    }

    public void AddRelic(string unitId, int amount)
    {
        unitGrowthSaveData.GetOrCreateUnit(unitId).relicCount += Mathf.Max(0, amount);
        OnGrowthDataChanged(false);
    }

    public void AddWishStone(string unitId, int amount)
    {
        unitGrowthSaveData.GetOrCreateUnit(unitId).wishStoneCount += Mathf.Max(0, amount);
        OnGrowthDataChanged(false);
    }

    public void AddEnhancementStone(int amount)
    {
        playerPassiveGrowthData.enhancementStoneCount += Mathf.Max(0, amount);
        OnGrowthDataChanged(false);
    }

    public void LoadGrowthData()
    {
        if (UnitGrowthSaveSystem.TryConsumeSceneTransferData(out UnitGrowthSaveContainer sceneTransferContainer))
        {
            ApplyGrowthData(sceneTransferContainer);
            return;
        }

        if (UnitGrowthSaveSystem.TryLoad(out UnitGrowthSaveContainer savedContainer))
        {
            ApplyGrowthData(savedContainer);
        }
    }

    public void SaveGrowthData()
    {
        RefreshPlayerPassiveTotals();
        UnitGrowthSaveSystem.Save(unitGrowthSaveData, playerPassiveGrowthData);
    }

    public void PrepareGrowthDataForSceneTransfer()
    {
        RefreshPlayerPassiveTotals();
        UnitGrowthSaveSystem.SetSceneTransferData(unitGrowthSaveData, playerPassiveGrowthData);
    }

    public void ClearSavedGrowthData()
    {
        UnitGrowthSaveSystem.Clear();
        unitGrowthSaveData = new UnitGrowthSaveData();
        playerPassiveGrowthData = new PlayerPassiveGrowthData();
        EnsureDefaultUnitGrowthData();
        RefreshPlayerPassiveTotals();
        RecalculateAllUnitStats();
    }

    public void ApplyGrowthData(UnitGrowthSaveContainer container)
    {
        if (container == null)
            return;

        unitGrowthSaveData = container.unitGrowthSaveData ?? new UnitGrowthSaveData();
        playerPassiveGrowthData = container.playerPassiveGrowthData ?? new PlayerPassiveGrowthData();
        EnsureDefaultUnitGrowthData();
        RefreshPlayerPassiveTotals();
        RecalculateAllUnitStats();
    }

    public UnitGrowthSaveContainer CreateCurrentSaveContainer()
    {
        RefreshPlayerPassiveTotals();
        return UnitGrowthSaveSystem.CreateContainer(unitGrowthSaveData, playerPassiveGrowthData);
    }

    public bool HasSavedGrowthData()
    {
        return UnitGrowthSaveSystem.HasSavedData();
    }

    private UnitData FindUnitData(string unitId)
    {
        if (unitDatabase == null)
            return null;

        foreach (UnitData unitData in unitDatabase)
        {
            if (unitData != null && unitData.unitId == unitId)
                return unitData;
        }

        return null;
    }

    private void EnsureDefaultUnitGrowthData()
    {
        if (unitGrowthSaveData == null)
            unitGrowthSaveData = new UnitGrowthSaveData();

        if (unitDatabase == null)
            return;

        bool changed = false;

        foreach (UnitData unitData in unitDatabase)
        {
            if (unitData == null || string.IsNullOrWhiteSpace(unitData.unitId))
                continue;

            if (unitGrowthSaveData.GetUnit(unitData.unitId) != null)
                continue;

            unitGrowthSaveData.units.Add(new UnitGrowthEntry
            {
                unitId = unitData.unitId,
                unitShardCount = 1,
                unitShardLevel = 1,
                magicStoneLevel = 0,
                equipmentLevel = 0,
                relicLevel = 0,
                skillLevel = 0
            });
            changed = true;
        }

        if (changed && saveDataOnChange)
            SaveGrowthData();
    }

    private bool CanPayGold(int goldCost)
    {
        if (useMainGoldForGrowthCost)
        {
            if (PlayerProgressManager.Instance != null)
                return PlayerProgressManager.Instance.playerProgressData.mainGold >= goldCost;

            return PlayerProgressSaveSystem.Data.mainGold >= goldCost;
        }

        return goldManager != null && goldManager.currentGold >= goldCost;
    }

    private bool SpendGrowthGold(int goldCost)
    {
        if (goldCost <= 0)
            return true;

        if (useMainGoldForGrowthCost)
        {
            if (PlayerProgressManager.Instance != null)
                return PlayerProgressManager.Instance.TrySpendMainGold(goldCost);

            return PlayerProgressSaveSystem.TrySpendMainGold(goldCost);
        }

        return goldManager != null && goldManager.UseGold(goldCost);
    }

    private void OnGrowthDataChanged(bool recalculateUnits)
    {
        RefreshPlayerPassiveTotals();

        if (saveDataOnChange)
            SaveGrowthData();

        if (recalculateUnits)
            RecalculateAllUnitStats();
    }

    private void RefreshPlayerPassiveTotals()
    {
        if (playerPassiveGrowthData == null || playerPassiveGrowthData.passives == null)
            return;

        float attackBonus = 0f;
        float attackSpeedBonus = 0f;

        foreach (PlayerPassiveGrowthEntry passive in playerPassiveGrowthData.passives)
        {
            if (passive == null)
                continue;

            PlayerPassiveUpgradeData data = UnitGrowthBalanceConfig.GetPlayerPassiveData(passive.level);
            attackBonus += data.attackBonus;
            attackSpeedBonus += data.attackSpeedBonus;
        }

        playerPassiveGrowthData.totalAttackBonus = attackBonus;
        playerPassiveGrowthData.totalAttackSpeedBonus = attackSpeedBonus;
    }

    private void RecalculateAllUnitStats()
    {
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        foreach (UnitController unit in units)
        {
            if (unit != null)
                unit.RecalculateStats();
        }
    }
}
