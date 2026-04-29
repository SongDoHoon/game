using System;

public static class MonsterBalanceCalculator
{
    private const double BaseNormalHp = 100.0;
    private const float MonsterMoveSpeedScale = 0.5f;

    private static readonly double[] NormalHpGrowthRates =
    {
        1.10,
        1.14,
        1.17,
        1.20,
        1.24,
        1.35,
        1.42,
        1.50,
        1.58,
        1.68
    };

    private static readonly double[] BossHpMultipliers =
    {
        8.0,
        12.0,
        18.0,
        27.0,
        45.0,
        70.0,
        110.0,
        170.0,
        260.0,
        400.0
    };

    private static readonly float[] NormalMoveSpeeds =
    {
        1.00f,
        1.05f,
        1.10f,
        1.16f,
        1.23f,
        1.35f,
        1.47f,
        1.60f,
        1.74f,
        1.90f
    };

    private static readonly float[] BossMoveSpeeds =
    {
        0.75f,
        0.78f,
        0.82f,
        0.86f,
        0.90f,
        0.96f,
        1.02f,
        1.08f,
        1.12f,
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

        return Math.Round(hp);
    }

    public static double GetBossHp(int wave)
    {
        return Math.Round(GetNormalMonsterHp(wave) * GetBossHpMultiplier(wave));
    }

    public static float GetNormalMoveSpeed(int wave)
    {
        return NormalMoveSpeeds[GetStageIndex(wave)] * MonsterMoveSpeedScale;
    }

    public static float GetBossMoveSpeed(int wave)
    {
        return BossMoveSpeeds[GetStageIndex(wave)] * MonsterMoveSpeedScale;
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

    private static int GetStageIndex(int wave)
    {
        int clampedWave = Math.Max(1, Math.Min(100, wave));
        return (clampedWave - 1) / 10;
    }
}
