using UnityEngine;

public class BattleMagicStoneManager : MonoBehaviour
{
    private const int MaxWorkerCount = 30;
    private const float MagicStonePerWorkerPerSecond = 0.8f;

    private static readonly int[] WorkerCosts =
    {
        15, 15, 15, 15, 15,
        18, 18, 18, 18, 18,
        24, 24, 24, 24, 24,
        32, 32, 32, 32, 32,
        42, 42, 42, 42, 42,
        55, 55, 55, 55, 55
    };

    public static BattleMagicStoneManager Instance { get; private set; }

    [Header("Gold")]
    public GoldManager goldManager;
    public WaveManager waveManager;

    [Header("Runtime")]
    [SerializeField] private double currentBattleMagicStone;
    [SerializeField] private int workerCount;

    public double CurrentBattleMagicStone => currentBattleMagicStone;
    public int WorkerCount => workerCount;
    public int MaxWorkers => MaxWorkerCount;
    public float MagicStonePerSecond => workerCount * MagicStonePerWorkerPerSecond;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();

        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();

        ResetForBattle();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (workerCount <= 0)
            return;

        if (IsProductionPaused())
            return;

        currentBattleMagicStone += MagicStonePerSecond * Time.deltaTime;
    }

    public void ResetForBattle()
    {
        currentBattleMagicStone = 0.0;
        workerCount = 0;
        GameModifierState.ResetBattleState();
    }

    public int GetNextWorkerCost()
    {
        if (workerCount >= MaxWorkerCount)
            return 0;

        return WorkerCosts[Mathf.Clamp(workerCount, 0, WorkerCosts.Length - 1)];
    }

    public bool CanHireWorker()
    {
        if (workerCount >= MaxWorkerCount)
            return false;

        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();

        return goldManager != null && goldManager.currentGold >= GetNextWorkerCost();
    }

    public bool TryHireWorker()
    {
        if (!CanHireWorker())
            return false;

        int cost = GetNextWorkerCost();
        if (!goldManager.UseGold(cost))
            return false;

        workerCount++;
        return true;
    }

    public int GetGroupLevel(UnitEnhanceGroup group)
    {
        return GameModifierState.GetEnhancementLevel(group);
    }

    public int GetNextUpgradeCost(UnitEnhanceGroup group)
    {
        int level = GetGroupLevel(group);
        if (level >= GameBalanceConfig.MaxEnhancementLevel)
            return 0;

        return GameModifierState.GetNextEnhancementCost(group);
    }

    public bool CanUpgradeGradeGroup(UnitEnhanceGroup group)
    {
        int level = GetGroupLevel(group);
        if (level >= GameBalanceConfig.MaxEnhancementLevel)
            return false;

        return currentBattleMagicStone >= GetNextUpgradeCost(group);
    }

    public bool TryUpgradeGradeGroup(UnitEnhanceGroup group)
    {
        if (!CanUpgradeGradeGroup(group))
            return false;

        int currentLevel = GetGroupLevel(group);
        int cost = GetNextUpgradeCost(group);
        currentBattleMagicStone -= cost;
        return GameModifierState.SetEnhancementLevel(group, currentLevel + 1);
    }

    public bool CanSpendBattleMagicStone(int amount)
    {
        return amount >= 0 && currentBattleMagicStone >= amount;
    }

    public bool TrySpendBattleMagicStone(int amount)
    {
        if (!CanSpendBattleMagicStone(amount))
            return false;

        currentBattleMagicStone -= amount;
        return true;
    }

    public void AddBattleMagicStone(int amount)
    {
        if (amount <= 0)
            return;

        currentBattleMagicStone += amount;
    }

    public double GetAttackMultiplier(UnitEnhanceGroup group)
    {
        int level = GetGroupLevel(group);
        return GameBalanceConfig.TryGetEnhancementData(group, level, out EnhancementLevelData data)
            ? data.attackPowerMultiplier
            : 1.0;
    }

    public float GetAttackSpeedMultiplier(UnitEnhanceGroup group)
    {
        int level = GetGroupLevel(group);
        return GameBalanceConfig.TryGetEnhancementData(group, level, out EnhancementLevelData data)
            ? data.attackSpeedMultiplier
            : 1f;
    }

    public UnitEnhanceGroup GetGroupByUnitGrade(UnitGrade grade)
    {
        return GameModifierState.GetEnhancementGroup(grade);
    }

    private bool IsProductionPaused()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();

        return waveManager != null && waveManager.isPausedForAuction;
    }
}
