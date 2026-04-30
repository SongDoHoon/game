using System;

[Serializable]
public class PlayerProgressSaveData
{
    public int mainGold;
    public int playerLevel = 1;
    public int playerExp;
    public int totalEarnedMainGold;
    public int totalEarnedPlayerExp;

    public void AddMainGold(int amount)
    {
        int safeAmount = Math.Max(0, amount);
        mainGold += safeAmount;
        totalEarnedMainGold += safeAmount;
    }

    public void AddPlayerExp(int amount)
    {
        int safeAmount = Math.Max(0, amount);
        playerExp += safeAmount;
        totalEarnedPlayerExp += safeAmount;
        RecalculateLevel();
    }

    public int GetRequiredExpForNextLevel()
    {
        return PlayerProgressionConfig.GetRequiredExpForNextLevel(playerLevel);
    }

    private void RecalculateLevel()
    {
        playerLevel = PlayerProgressionConfig.GetLevelFromTotalExp(playerExp);
    }
}
