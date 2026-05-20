using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class WaveManager : MonoBehaviour
{
    private static bool autoStartRequested;
    private static float requestedAutoStartDelay = 5f;

    [Header("Spawner")]
    public MonsterSpawner monsterSpawner;
    public BattleLifeManager battleLifeManager;
    public BountyManager bountyManager;
    public MissionManager missionManager;

    [Header("Wave Settings")]
    public int currentWave = 0;
    public bool waveStarted = false;
    public bool waitingForNextWave = false;
    public bool isPausedForAuction = false;
    public bool gameEnded = false;
    public float elapsedBattleTime = 0f;

    [Header("Spawn Count Per Wave")]
    public int normalMonsterCount = 15;
    public float normalSpawnSpacingDistance = 1f;
    public int finalWave = 100;

    [Header("Game Over Scene")]
    public bool returnToMainSceneOnGameOver = true;
    public string mainSceneName = "MainScene";
    public float returnToMainSceneDelay = 1.5f;

    private int aliveMonsterCount = 0;
    private Coroutine spawnWaveCoroutine;
    private Coroutine autoStartCoroutine;

    public bool IsBattleTimerRunning => waveStarted && !gameEnded;

    private void Start()
    {
        ResolveBattleLifeManager();

        if (!autoStartRequested)
            return;

        float delay = requestedAutoStartDelay;
        autoStartRequested = false;
        autoStartCoroutine = StartCoroutine(CoStartFirstWaveAfterDelay(delay));
    }

    public static void RequestAutoStartOnNextScene(float delay)
    {
        autoStartRequested = true;
        requestedAutoStartDelay = Mathf.Max(0f, delay);
    }

    public void StartFirstWave()
    {
        if (waveStarted) return;
        if (gameEnded) return;

        CancelAutoStart();
        ResetBattleRuntimeData();
        waveStarted = true;
        StartNextWave();
    }

    private void Update()
    {
        if (!IsBattleTimerRunning)
            return;

        elapsedBattleTime += Time.deltaTime;
    }

    public void StartNextWave()
    {
        if (monsterSpawner == null) return;
        if (isPausedForAuction) return;
        if (gameEnded) return;
        if (currentWave >= finalWave) return;

        CancelAutoStart();
        waveStarted = true;

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
        if (gameEnded)
            return;

        aliveMonsterCount--;

        if (aliveMonsterCount <= 0 && currentWave >= finalWave)
        {
            GrantStageClearGold();
            CompleteGame(true);
            return;
        }

        if (aliveMonsterCount <= 0 && !waitingForNextWave && !isPausedForAuction && currentWave < finalWave)
        {
            GrantStageClearGold();
            waitingForNextWave = true;
            Invoke(nameof(StartNextWave), 1.5f);
        }
    }

    public void NotifyMonsterReachedGoal()
    {
        if (gameEnded)
            return;

        ResolveBattleLifeManager();

        bool hasLifeRemaining = battleLifeManager != null && battleLifeManager.LoseLife();
        aliveMonsterCount--;

        if (!hasLifeRemaining)
        {
            CompleteGame(false);
            return;
        }

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

    private void GrantStageClearGold()
    {
        int goldAmount = GameBalanceConfig.GetStageClearGold() + GameModifierState.StageStartBonusGold;
        if (goldAmount <= 0)
            return;

        GoldManager goldManager = FindAnyObjectByType<GoldManager>();
        if (goldManager != null)
            goldManager.AddGold(goldAmount);
    }

    private void ResetBattleRuntimeData()
    {
        elapsedBattleTime = 0f;

        ResolveBattleLifeManager();
        if (battleLifeManager != null)
            battleLifeManager.ResetLife();

        BattleMagicStoneManager magicStoneManager = BattleMagicStoneManager.Instance;
        if (magicStoneManager == null)
            magicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        if (magicStoneManager != null)
            magicStoneManager.ResetForBattle();
        else
            GameModifierState.ResetBattleState();

        bountyManager = BountyManager.EnsureInstance(this, monsterSpawner);
        if (bountyManager != null)
            bountyManager.ResetBountyForBattleStart();

        if (missionManager == null)
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindAnyObjectByType<MissionManager>();

        if (missionManager != null)
            missionManager.InitializeMissions();
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

        CancelAutoStart();

        GameResultRewardManager rewardManager = GameResultRewardManager.Instance;
        if (rewardManager == null)
            rewardManager = FindAnyObjectByType<GameResultRewardManager>();

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

        if (!cleared && returnToMainSceneOnGameOver)
            StartCoroutine(CoReturnToMainSceneAfterDelay());
    }

    private IEnumerator CoStartFirstWaveAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        autoStartCoroutine = null;
        StartFirstWave();
    }

    private void CancelAutoStart()
    {
        if (autoStartCoroutine != null)
        {
            StopCoroutine(autoStartCoroutine);
            autoStartCoroutine = null;
        }
    }

    private void ResolveBattleLifeManager()
    {
        if (battleLifeManager == null)
            battleLifeManager = FindAnyObjectByType<BattleLifeManager>();
    }

    private IEnumerator CoReturnToMainSceneAfterDelay()
    {
        float delay = Mathf.Max(0f, returnToMainSceneDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!string.IsNullOrWhiteSpace(mainSceneName))
            SceneManager.LoadScene(mainSceneName);
    }

    public int GetElapsedBattleSeconds()
    {
        return Mathf.Max(0, Mathf.FloorToInt(elapsedBattleTime));
    }

    public string GetFormattedElapsedBattleTime()
    {
        int totalSeconds = GetElapsedBattleSeconds();
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
