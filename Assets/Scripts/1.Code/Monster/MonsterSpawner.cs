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
        {
            Debug.LogWarning("MonsterSpawner: monsterPrefab이 비어있음");
            return;
        }

        if (waypointPath == null)
        {
            Debug.LogWarning("MonsterSpawner: waypointPath가 비어있음");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject obj = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
        MonsterController monster = obj.GetComponent<MonsterController>();

        if (monster == null)
        {
            Debug.LogWarning("MonsterSpawner: 일반 몬스터 프리팹에 MonsterController가 없음");
            return;
        }

        monster.rewardGold = 10;
        monster.isBoss = false;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        ApplyWaveStat(monster, waveManager.currentWave, false);

        Debug.Log($"일반 몬스터 생성 / 웨이브 {waveManager.currentWave} / 체력 {monster.maxHp}");
    }

    public void SpawnBossForWave(WaveManager waveManager)
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("MonsterSpawner: bossPrefab이 비어있음");
            return;
        }

        if (waypointPath == null)
        {
            Debug.LogWarning("MonsterSpawner: waypointPath가 비어있음");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();

        GameObject obj = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        MonsterController monster = obj.GetComponent<MonsterController>();

        if (monster == null)
        {
            Debug.LogWarning("MonsterSpawner: 보스 프리팹에 MonsterController가 없음");
            return;
        }

        monster.rewardGold = 100;
        monster.isBoss = true;
        monster.SetPath(waypointPath);
        monster.SetWaveManager(waveManager);

        ApplyWaveStat(monster, waveManager.currentWave, true);

        Debug.Log($"보스 등장! / 웨이브 {waveManager.currentWave} / 체력 {monster.maxHp}");
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
