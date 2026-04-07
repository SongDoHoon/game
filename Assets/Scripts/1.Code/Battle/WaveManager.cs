using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawner")]
    public MonsterSpawner monsterSpawner;

    [Header("Wave Settings")]
    public int currentWave = 0;
    public bool waveStarted = false;
    public bool waitingForNextWave = false;

    [Header("Spawn Count Per Wave")]
    public int normalMonsterCount = 1;

    private int aliveMonsterCount = 0;

    public void StartFirstWave()
    {
        if (waveStarted) return;

        waveStarted = true;
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (monsterSpawner == null) return;

        currentWave++;
        waitingForNextWave = false;

        Debug.Log($"=== 웨이브 {currentWave} 시작 ===");

        if (currentWave % 10 == 0)
        {
            aliveMonsterCount = 1;
            monsterSpawner.SpawnBossForWave(this);
        }
        else
        {
            aliveMonsterCount = normalMonsterCount;

            for (int i = 0; i < normalMonsterCount; i++)
            {
                monsterSpawner.SpawnNormalForWave(this);
            }
        }
    }

    public void NotifyMonsterDead()
    {
        aliveMonsterCount--;
        Debug.Log($"남은 몬스터 수: {aliveMonsterCount}");

        if (aliveMonsterCount <= 0 && !waitingForNextWave)
        {
            waitingForNextWave = true;
            Invoke(nameof(StartNextWave), 1.5f);
        }
    }
}