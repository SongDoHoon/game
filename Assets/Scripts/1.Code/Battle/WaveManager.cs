using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Spawner")]
    public MonsterSpawner monsterSpawner;

    [Header("Wave Settings")]
    public int currentWave = 0;
    public bool waveStarted = false;
    public bool waitingForNextWave = false;
    public bool isPausedForAuction = false;

    [Header("Spawn Count Per Wave")]
    public int normalMonsterCount = 1;
    public float normalSpawnSpacingDistance = 1f;

    private int aliveMonsterCount = 0;
    private Coroutine spawnWaveCoroutine;

    public void StartFirstWave()
    {
        if (waveStarted) return;

        waveStarted = true;
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (monsterSpawner == null) return;
        if (isPausedForAuction) return;

        if (spawnWaveCoroutine != null)
        {
            StopCoroutine(spawnWaveCoroutine);
            spawnWaveCoroutine = null;
        }

        currentWave++;
        waitingForNextWave = false;

        if (currentWave % 10 == 0)
        {
            aliveMonsterCount = 1;
            monsterSpawner.SpawnBossForWave(this);
        }
        else
        {
            aliveMonsterCount = normalMonsterCount;
            spawnWaveCoroutine = StartCoroutine(CoSpawnNormalWave());
        }
    }

    public void NotifyMonsterDead()
    {
        aliveMonsterCount--;

        if (aliveMonsterCount <= 0 && !waitingForNextWave && !isPausedForAuction)
        {
            waitingForNextWave = true;
            Invoke(nameof(StartNextWave), 1.5f);
        }
    }

    public void PauseForAuction()
    {
        isPausedForAuction = true;
        waitingForNextWave = false;
        CancelInvoke(nameof(StartNextWave));

        if (spawnWaveCoroutine != null)
        {
            StopCoroutine(spawnWaveCoroutine);
            spawnWaveCoroutine = null;
        }
    }

    public void ResumeAfterAuction()
    {
        if (!isPausedForAuction)
            return;

        isPausedForAuction = false;

        if (aliveMonsterCount <= 0 && !waitingForNextWave)
        {
            waitingForNextWave = true;
            Invoke(nameof(StartNextWave), 1.5f);
        }
    }

    private IEnumerator CoSpawnNormalWave()
    {
        float spacingDistance = Mathf.Max(0f, normalSpawnSpacingDistance);

        for (int i = 0; i < normalMonsterCount; i++)
        {
            if (isPausedForAuction)
                yield break;

            MonsterController spawnedMonster = monsterSpawner.SpawnNormalForWave(this);

            if (i < normalMonsterCount - 1)
            {
                float moveSpeed = spawnedMonster != null ? Mathf.Max(0.01f, spawnedMonster.moveSpeed) : 1f;
                float spawnDelay = spacingDistance / moveSpeed;

                if (spawnDelay > 0f)
                    yield return new WaitForSeconds(spawnDelay);
            }
        }

        spawnWaveCoroutine = null;
    }
}
