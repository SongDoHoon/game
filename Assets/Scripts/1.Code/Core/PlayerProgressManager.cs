using UnityEngine;

public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance { get; private set; }

    [Header("Runtime Data")]
    public PlayerProgressSaveData playerProgressData = new();

    [Header("Debug View")]
    [SerializeField] private int debugMainGold;
    [SerializeField] private int debugPlayerLevel;
    [SerializeField] private int debugPlayerExp;
    [SerializeField] private int debugRequiredExpForNextLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgress();
    }

    public void LoadProgress()
    {
        playerProgressData = PlayerProgressSaveSystem.Load();
        PlayerProgressSaveSystem.Save(playerProgressData);
        RefreshDebugView();
    }

    public void SaveProgress()
    {
        PlayerProgressSaveSystem.Save(playerProgressData);
        RefreshDebugView();
    }

    public void AddMainGold(int amount)
    {
        playerProgressData.AddMainGold(amount);
        SaveProgress();
    }

    public void AddPlayerExp(int amount)
    {
        playerProgressData.AddPlayerExp(amount);
        SaveProgress();
    }

    public bool TrySpendMainGold(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (playerProgressData.mainGold < safeAmount)
            return false;

        playerProgressData.mainGold -= safeAmount;
        SaveProgress();
        return true;
    }

    public void AddGameReward(int mainGold, int playerExp)
    {
        playerProgressData.AddMainGold(mainGold);
        playerProgressData.AddPlayerExp(playerExp);
        SaveProgress();
    }

    [ContextMenu("Clear Player Progress")]
    public void ClearProgress()
    {
        PlayerProgressSaveSystem.Clear();
        playerProgressData = PlayerProgressSaveSystem.Data;
        RefreshDebugView();
    }

    private void RefreshDebugView()
    {
        if (playerProgressData == null)
            playerProgressData = new PlayerProgressSaveData();

        debugMainGold = playerProgressData.mainGold;
        debugPlayerLevel = playerProgressData.playerLevel;
        debugPlayerExp = playerProgressData.playerExp;
        debugRequiredExpForNextLevel = playerProgressData.GetRequiredExpForNextLevel();
    }
}
