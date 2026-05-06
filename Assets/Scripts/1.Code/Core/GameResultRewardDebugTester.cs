using UnityEngine;

public class GameResultRewardDebugTester : MonoBehaviour
{
    [Header("Reward Test")]
    public int testReachedWave = 5;
    public int testFinalWave = 100;
    public bool testCleared;

    [Header("Reward Values")]
    public int mainGoldPerWave = 10;
    public int playerExpPerWave = 15;
    public int clearMainGoldBonus = 300;
    public int clearPlayerExpBonus = 500;
    public float failedRunRewardMultiplier = 0.6f;

    [ContextMenu("1 Grant Test Game Result Reward")]
    public void GrantTestGameResultReward()
    {
        GameResultReward reward = GameResultRewardCalculator.Calculate(
            testReachedWave,
            testFinalWave,
            testCleared,
            mainGoldPerWave,
            playerExpPerWave,
            clearMainGoldBonus,
            clearPlayerExpBonus,
            failedRunRewardMultiplier);

        AddReward(reward);
        Debug.Log($"Granted test reward. cleared: {reward.cleared}, reachedWave: {reward.reachedWave}, mainGold: {reward.mainGold}, playerExp: {reward.playerExp}");
    }

    [ContextMenu("2 Grant Failed Wave 5 Reward")]
    public void GrantFailedWave5Reward()
    {
        testReachedWave = 5;
        testFinalWave = 100;
        testCleared = false;
        GrantTestGameResultReward();
    }

    [ContextMenu("3 Grant Cleared Final Wave Reward")]
    public void GrantClearedFinalWaveReward()
    {
        testReachedWave = testFinalWave;
        testCleared = true;
        GrantTestGameResultReward();
    }

    [ContextMenu("4 Print Player Progress")]
    public void PrintPlayerProgress()
    {
        PlayerProgressSaveData data = GetProgressData();
        Debug.Log(
            $"mainGold: {data.mainGold}, " +
            $"playerLevel: {data.playerLevel}, " +
            $"playerExp: {data.playerExp}, " +
            $"totalEarnedMainGold: {data.totalEarnedMainGold}, " +
            $"totalEarnedPlayerExp: {data.totalEarnedPlayerExp}");
    }

    [ContextMenu("5 Clear Player Progress")]
    public void ClearPlayerProgress()
    {
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.ClearProgress();
        else
            PlayerProgressSaveSystem.Clear();

        Debug.Log("Player progress cleared.");
    }

    private static void AddReward(GameResultReward reward)
    {
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddGameReward(reward.mainGold, reward.playerExp);
        else
            PlayerProgressSaveSystem.AddReward(reward.mainGold, reward.playerExp);
    }

    private static PlayerProgressSaveData GetProgressData()
    {
        if (PlayerProgressManager.Instance != null)
            return PlayerProgressManager.Instance.playerProgressData;

        return PlayerProgressSaveSystem.Data;
    }
}
