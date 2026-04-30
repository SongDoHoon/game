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
    public bool gameEnded = false;

    [Header("Spawn Count Per Wave")]
    public int normalMonsterCount = 1;
    public float normalSpawnSpacingDistance = 1f;
    public int finalWave = 100;

    private int aliveMonsterCount = 0;
    private Coroutine spawnWaveCoroutine;

    public void StartFirstWave()
    {
        if (waveStarted) return;
        if (gameEnded) return;

        waveStarted = true;
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (monsterSpawner == null) return;
        if (isPausedForAuction) return;
        if (gameEnded) return;
        if (currentWave >= finalWave) return;

        if (spawnWaveCoroutine != null)
        {
            StopCoroutine(spawnWaveCoroutine);
            spawnWaveCoroutine = null;
        }

        currentWave++;
        waitingForNextWave = false;
        GrantStageStartGold();

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
        if (gameEnded)
            return;

        aliveMonsterCount--;

        if (aliveMonsterCount <= 0 && currentWave >= finalWave)
        {
            CompleteGame(true);
            return;
        }

        if (aliveMonsterCount <= 0 && !waitingForNextWave && !isPausedForAuction && currentWave < finalWave)
        {
            waitingForNextWave = true;
            Invoke(nameof(StartNextWave), 1.5f);
        }
    }

    public void NotifyMonsterReachedGoal()
    {
        if (gameEnded)
            return;

        CompleteGame(false);
    }

    public void PauseForAuction()
    {
        if (gameEnded)
            return;

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
        if (gameEnded)
            return;

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

    private void GrantStageStartGold()
    {
        if (GameModifierState.StageStartBonusGold <= 0)
            return;

        GoldManager goldManager = FindFirstObjectByType<GoldManager>();
        if (goldManager != null)
            goldManager.AddGold(GameModifierState.StageStartBonusGold);
    }

    private void CompleteGame(bool cleared)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        waveStarted = false;
        waitingForNextWave = false;
        isPausedForAuction = false;
        CancelInvoke(nameof(StartNextWave));

        if (spawnWaveCoroutine != null)
        {
            StopCoroutine(spawnWaveCoroutine);
            spawnWaveCoroutine = null;
        }

        GameResultRewardManager rewardManager = GameResultRewardManager.Instance;
        if (rewardManager == null)
            rewardManager = FindFirstObjectByType<GameResultRewardManager>();

        if (rewardManager != null)
        {
            rewardManager.GrantGameResultReward(currentWave, finalWave, cleared);
        }
        else
        {
            GameResultReward reward = GameResultRewardCalculator.Calculate(
                currentWave,
                finalWave,
                cleared,
                10,
                15,
                300,
                500,
                0.6f);

            if (PlayerProgressManager.Instance != null)
                PlayerProgressManager.Instance.AddGameReward(reward.mainGold, reward.playerExp);
            else
                PlayerProgressSaveSystem.AddReward(reward.mainGold, reward.playerExp);
        }
    }
}
