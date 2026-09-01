using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BossWaveConfig
{
    public int waveNumber = 10;
    public double maxHp = 500.0;
    public float moveSpeed = 2f;
    public int rewardGold = 100;
}

public class MonsterSpawner : MonoBehaviour
{
    private static readonly string[] BountyNames =
    {
        "고블린",
        "슬라임",
        "성전기사",
        "골렘",
        "처형관",
        "감시군주"
    };

    [Header("Spawn Settings")]
    public GameObject monsterPrefab;
    public GameObject bossPrefab;
    public WaypointPath waypointPath;

    [Header("Bounty Spine Appearances")]
    public BountySpineAppearance[] bountySpineAppearances =
        new BountySpineAppearance[BountyManager.MaxBountyDifficulty];

    [Header("Wave Scaling")]
    public float normalHpMultiplierPerWave = 0.15f;
    public float bossHpMultiplierPerWave = 0.3f;

    [Header("Boss Wave Configs")]
    public List<BossWaveConfig> bossWaveConfigs = new();

    private void Awake()
    {
        EnsureBossWaveConfigs();
        EnsureBountySpineAppearances();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureBossWaveConfigs();
        EnsureBountySpineAppearances();
    }
#endif

    public MonsterController SpawnNormalForWave(WaveManager waveManager)
    {
        if (monsterPrefab == null)
            return null;

        if (waypointPath == null)
            return null;

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject obj = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
        obj.transform.localScale = Vector3.one * 0.5f;
        MonsterController monster = obj.GetComponent<MonsterController>();

        if (monster == null)
            return null;

        monster.rewardGold = GameBalanceConfig.GetNormalKillGold();
        monster.isBoss = false;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        int wave = waveManager != null ? waveManager.currentWave : 1;
        ApplyWaveStat(monster, wave, false);
        monster.SetAppearanceForWave(wave);
        return monster;
    }

    public void SpawnBossForWave(WaveManager waveManager)
    {
        if (bossPrefab == null)
            return;

        if (waypointPath == null)
            return;

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject obj = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        obj.transform.localScale = Vector3.one * 0.5f;
        MonsterController monster = obj.GetComponent<MonsterController>();

        if (monster == null)
            return;

        monster.isBoss = true;
        monster.monsterType = MonsterType.Boss;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        ApplyBossStat(monster, waveManager != null ? waveManager.currentWave : 0);
    }

    public MonsterController SpawnBountyElite(BountyEliteData data, BountyManager bountyManager)
    {
        if (data == null)
            return null;

        GameObject prefab = bossPrefab != null ? bossPrefab : monsterPrefab;
        if (prefab == null)
            return null;

        if (waypointPath == null)
            return null;

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        obj.transform.localScale = Vector3.one * 0.5f;
        MonsterController monster = obj.GetComponent<MonsterController>();

        if (monster == null)
            return null;

        monster.monsterType = MonsterType.BountyElite;
        monster.isBoss = false;
        monster.rewardGold = 0;
        monster.maxHp = data.hp;
        monster.currentHp = monster.maxHp;
        monster.moveSpeed = MonsterBalanceCalculator.GetNormalMoveSpeed(1);
        monster.bountyDifficulty = data.difficulty;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(bountyManager != null ? bountyManager.waveManager : null);
        monster.SetBountyManager(bountyManager);

        if (prefab == monsterPrefab)
            monster.SetAppearanceForWave(1);

        ApplyBountySpineAppearance(monster, data.difficulty);

        return monster;
    }

    private void ApplyBountySpineAppearance(MonsterController monster, int difficulty)
    {
        if (monster == null)
            return;

        BountySpineAppearance appearance = GetBountySpineAppearance(difficulty);
        if (appearance == null || appearance.skeletonData == null)
            return;

        BountySpineAnimationController controller =
            BountySpineAnimationController.GetOrCreate(monster);

        if (controller != null)
            controller.Configure(monster, appearance);
    }

    private BountySpineAppearance GetBountySpineAppearance(int difficulty)
    {
        if (bountySpineAppearances == null)
            return null;

        foreach (BountySpineAppearance appearance in bountySpineAppearances)
        {
            if (appearance != null && appearance.difficulty == difficulty)
                return appearance;
        }

        return null;
    }

    private void ApplyWaveStat(MonsterController monster, int wave, bool isBoss)
    {
        if (monster == null)
            return;

        if (isBoss)
        {
            monster.maxHp = MonsterBalanceCalculator.GetBossHp(wave);
            monster.currentHp = monster.maxHp;
            monster.moveSpeed = MonsterBalanceCalculator.GetBossMoveSpeed(wave);
            monster.rewardGold = MonsterBalanceCalculator.GetBossClearGold(wave);
            return;
        }

        monster.maxHp = MonsterBalanceCalculator.GetNormalMonsterHp(wave);
        monster.currentHp = monster.maxHp;
        monster.moveSpeed = MonsterBalanceCalculator.GetNormalMoveSpeed(wave);
        monster.rewardGold = GameBalanceConfig.GetNormalKillGold();
    }

    private void ApplyBossStat(MonsterController monster, int wave)
    {
        if (monster == null)
            return;

        ApplyWaveStat(monster, wave, true);
    }

    private BossWaveConfig GetBossWaveConfig(int wave)
    {
        if (bossWaveConfigs == null || bossWaveConfigs.Count == 0)
            return null;

        foreach (BossWaveConfig config in bossWaveConfigs)
        {
            if (config == null)
                continue;

            if (config.waveNumber == wave)
                return config;
        }

        int bossIndex = (Mathf.Max(10, wave) / 10) - 1;
        if (bossIndex >= 0 && bossIndex < bossWaveConfigs.Count)
            return bossWaveConfigs[bossIndex];

        return null;
    }

    private void EnsureBossWaveConfigs()
    {
        if (bossWaveConfigs == null)
            bossWaveConfigs = new List<BossWaveConfig>();

        while (bossWaveConfigs.Count < 10)
            bossWaveConfigs.Add(CreateDefaultBossWaveConfig(bossWaveConfigs.Count));

        if (bossWaveConfigs.Count > 10)
            bossWaveConfigs.RemoveRange(10, bossWaveConfigs.Count - 10);

        for (int i = 0; i < bossWaveConfigs.Count; i++)
        {
            if (bossWaveConfigs[i] == null)
                bossWaveConfigs[i] = CreateDefaultBossWaveConfig(i);

            bossWaveConfigs[i].waveNumber = (i + 1) * 10;
        }
    }

    private void EnsureBountySpineAppearances()
    {
        int appearanceCount = BountyManager.MaxBountyDifficulty;

        if (bountySpineAppearances == null || bountySpineAppearances.Length != appearanceCount)
        {
            BountySpineAppearance[] resizedAppearances =
                new BountySpineAppearance[appearanceCount];

            if (bountySpineAppearances != null)
            {
                int copyCount = Mathf.Min(bountySpineAppearances.Length, resizedAppearances.Length);
                for (int i = 0; i < copyCount; i++)
                    resizedAppearances[i] = bountySpineAppearances[i];
            }

            bountySpineAppearances = resizedAppearances;
        }

        for (int i = 0; i < bountySpineAppearances.Length; i++)
        {
            if (bountySpineAppearances[i] == null)
                bountySpineAppearances[i] = new BountySpineAppearance();

            bountySpineAppearances[i].difficulty = i + BountyManager.MinBountyDifficulty;
            bountySpineAppearances[i].bountyName = BountyNames[i];
        }
    }

    private BossWaveConfig CreateDefaultBossWaveConfig(int index)
    {
        MonsterController bossTemplate = bossPrefab != null ? bossPrefab.GetComponent<MonsterController>() : null;

        return new BossWaveConfig
        {
            waveNumber = (index + 1) * 10,
            maxHp = bossTemplate != null ? bossTemplate.maxHp : 500.0,
            moveSpeed = bossTemplate != null ? bossTemplate.moveSpeed : 2f,
            rewardGold = bossTemplate != null ? bossTemplate.rewardGold : 100
        };
    }

    private Vector3 GetSpawnPosition()
    {
        if (waypointPath != null && waypointPath.GetWaypoint(0) != null)
            return waypointPath.GetWaypoint(0).position;

        return Vector3.zero;
    }
}
