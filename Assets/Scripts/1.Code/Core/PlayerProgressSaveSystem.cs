using UnityEngine;

public static class PlayerProgressSaveSystem
{
    private const string SaveKey = "PlayerProgressSaveData";

    private static PlayerProgressSaveData cachedData;

    public static PlayerProgressSaveData Data
    {
        get
        {
            if (cachedData == null)
                cachedData = Load();

            return cachedData;
        }
    }

    public static PlayerProgressSaveData Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return new PlayerProgressSaveData();

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrWhiteSpace(json))
            return new PlayerProgressSaveData();

        PlayerProgressSaveData data = JsonUtility.FromJson<PlayerProgressSaveData>(json);
        return data ?? new PlayerProgressSaveData();
    }

    public static void Save(PlayerProgressSaveData data)
    {
        cachedData = data ?? new PlayerProgressSaveData();
        string json = JsonUtility.ToJson(cachedData);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static void AddReward(int mainGold, int playerExp)
    {
        PlayerProgressSaveData data = Data;
        data.AddMainGold(mainGold);
        data.AddPlayerExp(playerExp);
        Save(data);
    }

    public static bool TrySpendMainGold(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        PlayerProgressSaveData data = Data;

        if (data.mainGold < safeAmount)
            return false;

        data.mainGold -= safeAmount;
        Save(data);
        return true;
    }

    public static void Clear()
    {
        cachedData = new PlayerProgressSaveData();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
