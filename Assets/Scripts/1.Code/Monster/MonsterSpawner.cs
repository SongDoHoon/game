using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BossWaveConfig
{
    public int waveNumber = 10;
    public float maxHp = 500f;
    public float moveSpeed = 2f;
    public int rewardGold = 100;
}

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject monsterPrefab;
    public GameObject bossPrefab;
    public WaypointPath waypointPath;

    [Header("Optional Spawn Point")]
    public Transform spawnPoint;

    [Header("Wave Scaling")]
    public float normalHpMultiplierPerWave = 0.15f;
    public float bossHpMultiplierPerWave = 0.3f;

    [Header("Boss Wave Configs")]
    public List<BossWaveConfig> bossWaveConfigs = new();

    private void Awake()
    {
        EnsureBossWaveConfigs();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureBossWaveConfigs();
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

        monster.rewardGold = 10;
        monster.isBoss = false;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        ApplyWaveStat(monster, waveManager.currentWave, false);
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

    private void ApplyWaveStat(MonsterController monster, int wave, bool isBoss)
    {
        float multiplier;

        if (isBoss)
            multiplier = 1f + ((wave - 1) * bossHpMultiplierPerWave);
        else
            multiplier = 1f + ((wave - 1) * normalHpMultiplierPerWave);

        monster.maxHp *= multiplier;
        monster.currentHp = monster.maxHp;
    }

    private void ApplyBossStat(MonsterController monster, int wave)
    {
        if (monster == null)
            return;

        BossWaveConfig config = GetBossWaveConfig(wave);

        if (config == null)
        {
            monster.rewardGold = 100;
            ApplyWaveStat(monster, wave, true);
            return;
        }

        monster.maxHp = Mathf.Max(1f, config.maxHp);
        monster.currentHp = monster.maxHp;
        monster.moveSpeed = Mathf.Max(0.01f, config.moveSpeed);
        monster.rewardGold = Mathf.Max(0, config.rewardGold);
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

    private BossWaveConfig CreateDefaultBossWaveConfig(int index)
    {
        MonsterController bossTemplate = bossPrefab != null ? bossPrefab.GetComponent<MonsterController>() : null;

        return new BossWaveConfig
        {
            waveNumber = (index + 1) * 10,
            maxHp = bossTemplate != null ? bossTemplate.maxHp : 500f,
            moveSpeed = bossTemplate != null ? bossTemplate.moveSpeed : 2f,
            rewardGold = bossTemplate != null ? bossTemplate.rewardGold : 100
        };
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        if (waypointPath != null && waypointPath.GetWaypoint(0) != null)
            return waypointPath.GetWaypoint(0).position;

        return Vector3.zero;
    }
}
