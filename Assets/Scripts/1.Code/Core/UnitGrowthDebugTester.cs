using UnityEngine;

public class UnitGrowthDebugTester : MonoBehaviour
{
    public string testUnitId;
    public UnitData testUnitData;

    [ContextMenu("1 Add Unit Shards")]
    public void AddUnitShards()
    {
        if (!HasGrowthManager())
            return;

        UnitGrowthManager.Instance.AddUnitShard(testUnitId, 10);
        Debug.Log($"Added 10 unit shards to {testUnitId}.");
    }

    [ContextMenu("2 Try Upgrade Unit Shard")]
    public void TryUpgradeUnitShard()
    {
        if (!HasGrowthManager())
            return;

        bool success = UnitGrowthManager.Instance.TryUpgradeUnitShard(testUnitId);
        Debug.Log($"TryUpgradeUnitShard({testUnitId}): {success}");
    }

    [ContextMenu("3 Save Growth Data")]
    public void SaveGrowthData()
    {
        if (!HasGrowthManager())
            return;

        UnitGrowthManager.Instance.SaveGrowthData();
        Debug.Log("Growth data saved.");
    }

    [ContextMenu("4 Load Growth Data")]
    public void LoadGrowthData()
    {
        if (!HasGrowthManager())
            return;

        UnitGrowthManager.Instance.LoadGrowthData();
        Debug.Log("Growth data loaded.");
    }

    [ContextMenu("5 Print Growth Data")]
    public void PrintGrowthData()
    {
        if (!HasGrowthManager())
            return;

        UnitGrowthEntry growth = UnitGrowthManager.Instance.GetUnitGrowth(testUnitId);
        if (growth == null)
        {
            Debug.Log($"No growth data for {testUnitId}.");
            return;
        }

        Debug.Log(
            $"unitId: {growth.unitId}, " +
            $"shards: {growth.unitShardCount}, " +
            $"shardLevel: {growth.unitShardLevel}, " +
            $"magicStones: {growth.magicStoneCount}, " +
            $"magicStoneLevel: {growth.magicStoneLevel}, " +
            $"equipmentLevel: {growth.equipmentLevel}, " +
            $"relics: {growth.relicCount}, " +
            $"relicLevel: {growth.relicLevel}, " +
            $"wishStones: {growth.wishStoneCount}, " +
            $"skillLevel: {growth.skillLevel}");
    }

    [ContextMenu("6 Print Final Stats")]
    public void PrintFinalStats()
    {
        if (!HasGrowthManager())
            return;

        if (testUnitData == null)
        {
            Debug.Log("testUnitData is not assigned.");
            return;
        }

        UnitStatModifierResult result = UnitGrowthManager.Instance.GetStatResult(testUnitData);
        Debug.Log(
            $"unitId: {testUnitData.unitId}, " +
            $"baseAttack: {result.baseAttack}, " +
            $"finalAttack: {result.finalAttack}, " +
            $"baseAttackInterval: {result.baseAttackInterval}, " +
            $"finalAttackInterval: {result.finalAttackInterval}, " +
            $"inGameGradeMultiplier: {result.inGameGradeMultiplier}, " +
            $"magicStoneMultiplier: {result.magicStoneMultiplier}, " +
            $"equipmentMultiplier: {result.equipmentMultiplier}, " +
            $"relicMultiplier: {result.relicMultiplier}, " +
            $"additiveAttackBonus: {result.additiveAttackBonus}, " +
            $"attackSpeedMultiplier: {result.attackSpeedMultiplier}, " +
            $"additiveAttackSpeedBonus: {result.additiveAttackSpeedBonus}");
    }

    [ContextMenu("7 Clear Saved Growth Data")]
    public void ClearSavedGrowthData()
    {
        if (!HasGrowthManager())
            return;

        UnitGrowthManager.Instance.ClearSavedGrowthData();
        Debug.Log("Saved growth data cleared.");
    }

    [ContextMenu("8 Clear All Growth And Player Progress")]
    public void ClearAllGrowthAndPlayerProgress()
    {
        if (UnitGrowthManager.Instance != null)
            UnitGrowthManager.Instance.ClearSavedGrowthData();
        else
            UnitGrowthSaveSystem.Clear();

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.ClearProgress();
        else
            PlayerProgressSaveSystem.Clear();

        Debug.Log("All growth and player progress data cleared.");
    }

    private bool HasGrowthManager()
    {
        if (UnitGrowthManager.Instance != null)
            return true;

        Debug.Log("UnitGrowthManager.Instance is null.");
        return false;
    }
}
