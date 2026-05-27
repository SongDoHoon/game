using System;

public static class MonsterBalanceCalculator
{
    private const double BaseNormalHp = 100.0;
    private const double NormalHpDifficultyMultiplier = 1.0;
    private const double BossHpDifficultyMultiplier = 1.0;
    private const float MoveSpeedDifficultyMultiplier = 1f;
    private const float MonsterMoveSpeedScale = 0.5f;

    private static readonly double[] NormalHpGrowthRates =
    {
        1.08,
        1.10,
        1.145,
        1.155,
        1.18,
        1.14,
        1.09,
        1.08,
        1.06,
        1.05
    };

    private static readonly double[] BossHpMultipliers =
    {
        5.0,
        8.0,
        15.0,
        18.0,
        24.0,
        18.0,
        22.0,
        28.0,
        36.0,
        48.0
    };

    private static readonly double[] BossHpReliefMultipliers =
    {
        1.0,
        1.0,
        1.0,
        1.0,
        0.50,
        0.27,
        0.12,
        0.055,
        0.028,
        0.0145
    };

    private static readonly float[] NormalMoveSpeeds =
    {
        1.00f,
        1.03f,
        1.06f,
        1.10f,
        1.15f,
        1.23f,
        1.30f,
        1.38f,
        1.48f,
        1.60f
    };

    private static readonly float[] BossMoveSpeeds =
    {
        0.75f,
        0.78f,
        0.80f,
        0.84f,
        0.88f,
        0.94f,
        0.98f,
        1.03f,
        1.08f,
        1.15f
    };

    private static readonly int[] BossClearGoldByStage =
    {
        50,
        100,
        200,
        250,
        300,
        350,
        400,
        450,
        500,
        0
    };

    public static bool IsBossWave(int wave)
    {
        return wave > 0 && wave % 10 == 0;
    }

    public static double GetNormalMonsterHp(int wave)
    {
        int clampedWave = Math.Max(1, wave);
        double hp = BaseNormalHp;

        for (int currentWave = 2; currentWave <= clampedWave; currentWave++)
            hp *= GetNormalHpGrowthRate(currentWave);

        return Math.Round(hp * NormalHpDifficultyMultiplier);
    }

    public static double GetBossHp(int wave)
    {
        return Math.Round(GetNormalMonsterHp(wave) * GetBossHpMultiplier(wave) * GetBossHpReliefMultiplier(wave) * BossHpDifficultyMultiplier);
    }

    public static float GetNormalMoveSpeed(int wave)
    {
        return NormalMoveSpeeds[GetStageIndex(wave)] * MonsterMoveSpeedScale * MoveSpeedDifficultyMultiplier;
    }

    public static float GetBossMoveSpeed(int wave)
    {
        return BossMoveSpeeds[GetStageIndex(wave)] * MonsterMoveSpeedScale * MoveSpeedDifficultyMultiplier;
    }

    public static int GetBossClearGold(int wave)
    {
        if (!IsBossWave(wave))
            return 0;

        return BossClearGoldByStage[GetStageIndex(wave)];
    }

    private static double GetNormalHpGrowthRate(int wave)
    {
        return NormalHpGrowthRates[GetStageIndex(wave)];
    }

    private static double GetBossHpMultiplier(int wave)
    {
        return BossHpMultipliers[GetStageIndex(wave)];
    }

    private static double GetBossHpReliefMultiplier(int wave)
    {
        return BossHpReliefMultipliers[GetStageIndex(wave)];
    }

    private static int GetStageIndex(int wave)
    {
        int clampedWave = Math.Max(1, Math.Min(100, wave));
        return (clampedWave - 1) / 10;
    }
}
