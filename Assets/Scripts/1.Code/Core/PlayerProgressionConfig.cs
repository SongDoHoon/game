using System;

public static class PlayerProgressionConfig
{
    public const int MinPlayerLevel = 1;
    public const int MaxPlayerLevel = 100;

    public static int GetRequiredExpForNextLevel(int currentLevel)
    {
        int clampedLevel = Math.Max(MinPlayerLevel, Math.Min(MaxPlayerLevel, currentLevel));
        if (clampedLevel >= MaxPlayerLevel)
            return 0;

        return 100 + ((clampedLevel - 1) * 35);
    }

    public static int GetLevelFromTotalExp(int totalExp)
    {
        int safeExp = Math.Max(0, totalExp);
        int level = MinPlayerLevel;
        int spentExp = 0;

        while (level < MaxPlayerLevel)
        {
            int requiredExp = GetRequiredExpForNextLevel(level);
            if (requiredExp <= 0 || spentExp + requiredExp > safeExp)
                break;

            spentExp += requiredExp;
            level++;
        }

        return level;
    }
}
