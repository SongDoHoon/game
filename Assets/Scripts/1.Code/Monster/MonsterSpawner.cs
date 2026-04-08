using UnityEngine;

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

    public void SpawnNormalForWave(WaveManager waveManager)
    {
        if (monsterPrefab == null)
            return;

        if (waypointPath == null)
            return;

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject obj = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
        obj.transform.localScale = Vector3.one * 0.5f;
        MonsterController monster = obj.GetComponent<MonsterController>();

        if (monster == null)
            return;

        monster.rewardGold = 10;
        monster.isBoss = false;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        ApplyWaveStat(monster, waveManager.currentWave, false);
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

        monster.rewardGold = 100;
        monster.isBoss = true;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        ApplyWaveStat(monster, waveManager.currentWave, true);
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

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        if (waypointPath != null && waypointPath.GetWaypoint(0) != null)
            return waypointPath.GetWaypoint(0).position;

        return Vector3.zero;
    }
}
