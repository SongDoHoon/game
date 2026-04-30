using UnityEngine;

public class GameResultRewardManager : MonoBehaviour
{
    public static GameResultRewardManager Instance { get; private set; }

    [Header("Reward Per Cleared Wave")]
    public int mainGoldPerWave = 10;
    public int playerExpPerWave = 15;

    [Header("Clear Bonus")]
    public int clearMainGoldBonus = 300;
    public int clearPlayerExpBonus = 500;

    [Header("Failure Bonus")]
    public float failedRunRewardMultiplier = 0.6f;

    [Header("Runtime Result")]
    [SerializeField] private bool rewardGranted;
    [SerializeField] private bool lastRunCleared;
    [SerializeField] private int lastReachedWave;
    [SerializeField] private int lastMainGoldReward;
    [SerializeField] private int lastPlayerExpReward;

    public bool RewardGranted => rewardGranted;
    public bool LastRunCleared => lastRunCleared;
    public int LastReachedWave => lastReachedWave;
    public int LastMainGoldReward => lastMainGoldReward;
    public int LastPlayerExpReward => lastPlayerExpReward;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public GameResultReward GrantGameResultReward(int reachedWave, int finalWave, bool cleared)
    {
        if (rewardGranted)
            return GetLastReward();

        GameResultReward reward = GameResultRewardCalculator.Calculate(
            reachedWave,
            finalWave,
            cleared,
            mainGoldPerWave,
            playerExpPerWave,
            clearMainGoldBonus,
            clearPlayerExpBonus,
            failedRunRewardMultiplier);

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddGameReward(reward.mainGold, reward.playerExp);
        else
            PlayerProgressSaveSystem.AddReward(reward.mainGold, reward.playerExp);

        rewardGranted = true;
        lastRunCleared = cleared;
        lastReachedWave = reward.reachedWave;
        lastMainGoldReward = reward.mainGold;
        lastPlayerExpReward = reward.playerExp;

        return reward;
    }

    public void ResetRuntimeResult()
    {
        rewardGranted = false;
        lastRunCleared = false;
        lastReachedWave = 0;
        lastMainGoldReward = 0;
        lastPlayerExpReward = 0;
    }

    private GameResultReward GetLastReward()
    {
        return new GameResultReward
        {
            cleared = lastRunCleared,
            reachedWave = lastReachedWave,
            mainGold = lastMainGoldReward,
            playerExp = lastPlayerExpReward
        };
    }
}

public static class GameResultRewardCalculator
{
    public static GameResultReward Calculate(
        int reachedWave,
        int finalWave,
        bool cleared,
        int mainGoldPerWave,
        int playerExpPerWave,
        int clearMainGoldBonus,
        int clearPlayerExpBonus,
        float failedRunRewardMultiplier)
    {
        int safeFinalWave = Mathf.Max(1, finalWave);
        int safeReachedWave = Mathf.Clamp(reachedWave, 0, safeFinalWave);

        float rewardMultiplier = cleared ? 1f : Mathf.Clamp01(failedRunRewardMultiplier);
        int mainGold = Mathf.RoundToInt(safeReachedWave * Mathf.Max(0, mainGoldPerWave) * rewardMultiplier);
        int playerExp = Mathf.RoundToInt(safeReachedWave * Mathf.Max(0, playerExpPerWave) * rewardMultiplier);

        if (cleared)
        {
            mainGold += Mathf.Max(0, clearMainGoldBonus);
            playerExp += Mathf.Max(0, clearPlayerExpBonus);
        }

        return new GameResultReward
        {
            cleared = cleared,
            reachedWave = safeReachedWave,
            mainGold = mainGold,
            playerExp = playerExp
        };
    }
}

public struct GameResultReward
{
    public bool cleared;
    public int reachedWave;
    public int mainGold;
    public int playerExp;
}
