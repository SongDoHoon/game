using UnityEngine;

public class BountyManager : MonoBehaviour
{
    public const int MinBountyDifficulty = 1;
    public const int MaxBountyDifficulty = 6;

    private const float BountyCooldown = 70f;

    private static readonly BountyEliteData[] BountyData =
    {
        new BountyEliteData(1, 750.0, 80, 0, "현상금 난이도 1"),
        new BountyEliteData(2, 4000.0, 0, 100, "현상금 난이도 2"),
        new BountyEliteData(3, 120000.0, 150, 200, "현상금 난이도 3"),
        new BountyEliteData(4, 2200000.0, 220, 300, "현상금 난이도 4"),
        new BountyEliteData(5, 18000000.0, 320, 400, "현상금 난이도 5"),
        new BountyEliteData(6, 150000000.0, 400, 500, "현상금 난이도 6")
    };

    public static BountyManager Instance { get; private set; }

    [Header("References")]
    public WaveManager waveManager;
    public MonsterSpawner monsterSpawner;
    public GoldManager goldManager;
    public BattleMagicStoneManager battleMagicStoneManager;

    [Header("Runtime")]
    [SerializeField] private float bountyTimer;
    [SerializeField] private bool bountyReady;
    [SerializeField] private int unlockedBountyDifficulty = MinBountyDifficulty;
    [SerializeField] private MonsterController activeBountyElite;
    [SerializeField] private int activeBountyDifficulty;

    public bool HasActiveBountyElite => activeBountyElite != null && activeBountyElite.IsAlive;
    public int ActiveBountyDifficulty => HasActiveBountyElite ? activeBountyDifficulty : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsBattleRunning())
            return;

        if (bountyReady)
            return;

        bountyTimer += Time.deltaTime;
        if (bountyTimer >= BountyCooldown)
            bountyReady = true;
    }

    public static BountyManager EnsureInstance(WaveManager waveManager, MonsterSpawner monsterSpawner)
    {
        BountyManager manager = Instance != null ? Instance : Object.FindAnyObjectByType<BountyManager>();

        if (manager == null)
        {
            GameObject obj = new GameObject("BountyManager");
            manager = obj.AddComponent<BountyManager>();
        }

        if (waveManager != null)
            manager.waveManager = waveManager;

        if (monsterSpawner != null)
            manager.monsterSpawner = monsterSpawner;

        manager.ResolveReferences();
        return manager;
    }

    public bool IsBountyReady()
    {
        return bountyReady;
    }

    public int GetUnlockedBountyDifficulty()
    {
        return unlockedBountyDifficulty;
    }

    public bool CanSpawnBounty(int difficulty)
    {
        if (!IsBattleRunning())
            return false;

        if (!bountyReady || HasActiveBountyElite)
            return false;

        if (difficulty < MinBountyDifficulty || difficulty > unlockedBountyDifficulty)
            return false;

        return GetBountyData(difficulty) != null;
    }

    public bool TrySpawnBounty(int difficulty)
    {
        if (!CanSpawnBounty(difficulty))
            return false;

        ResolveReferences();

        if (monsterSpawner == null)
            return false;

        BountyEliteData data = GetBountyData(difficulty);
        MonsterController spawned = monsterSpawner.SpawnBountyElite(data, this);
        if (spawned == null)
            return false;

        activeBountyElite = spawned;
        activeBountyDifficulty = difficulty;
        bountyTimer = 0f;
        bountyReady = false;
        return true;
    }

    public void OnBountyEliteKilled(int difficulty)
    {
        BountyEliteData data = GetBountyData(difficulty);
        if (data == null)
            return;

        GrantBountyReward(data);
        UnlockNextDifficulty(difficulty);
        ClearActiveBounty();
    }

    public void OnBountyEliteRemoved(MonsterController monster)
    {
        if (monster == null || monster != activeBountyElite)
            return;

        ClearActiveBounty();
    }

    public BountyEliteData GetBountyData(int difficulty)
    {
        foreach (BountyEliteData data in BountyData)
        {
            if (data != null && data.difficulty == difficulty)
                return data;
        }

        return null;
    }

    public float GetRemainingBountyCooldown()
    {
        if (bountyReady)
            return 0f;

        return Mathf.Max(0f, BountyCooldown - bountyTimer);
    }

    public float GetBountyCooldownDuration()
    {
        return BountyCooldown;
    }

    public bool IsBountyEliteAlive()
    {
        return HasActiveBountyElite;
    }

    public void ResetBountyForBattleStart()
    {
        bountyTimer = 0f;
        bountyReady = true;
        unlockedBountyDifficulty = MinBountyDifficulty;
        activeBountyElite = null;
        activeBountyDifficulty = 0;
        ResolveReferences();
    }

    private void GrantBountyReward(BountyEliteData data)
    {
        ResolveReferences();

        if (goldManager != null && data.rewardGold > 0)
            goldManager.AddGold(data.rewardGold);

        if (battleMagicStoneManager != null && data.rewardBattleMagicStone > 0)
            battleMagicStoneManager.AddBattleMagicStone(data.rewardBattleMagicStone);
    }

    private void UnlockNextDifficulty(int clearedDifficulty)
    {
        if (clearedDifficulty < unlockedBountyDifficulty)
            return;

        if (unlockedBountyDifficulty >= MaxBountyDifficulty)
            return;

        unlockedBountyDifficulty = Mathf.Clamp(clearedDifficulty + 1, MinBountyDifficulty, MaxBountyDifficulty);
    }

    private void ClearActiveBounty()
    {
        activeBountyElite = null;
        activeBountyDifficulty = 0;
    }

    private bool IsBattleRunning()
    {
        ResolveReferences();
        return waveManager != null && waveManager.waveStarted && !waveManager.gameEnded;
    }

    private void ResolveReferences()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();

        if (monsterSpawner == null)
            monsterSpawner = FindAnyObjectByType<MonsterSpawner>();

        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();
    }
}
