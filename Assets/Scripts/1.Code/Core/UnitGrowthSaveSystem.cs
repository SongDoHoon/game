using System;
using UnityEngine;

public static class UnitGrowthSaveSystem
{
    private const string SaveKey = "UnitGrowthSaveData";

    private static UnitGrowthSaveContainer sceneTransferData;

    public static bool HasSavedData()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public static void Save(UnitGrowthSaveData unitGrowthSaveData, PlayerPassiveGrowthData playerPassiveGrowthData)
    {
        UnitGrowthSaveContainer container = CreateContainer(unitGrowthSaveData, playerPassiveGrowthData);
        string json = JsonUtility.ToJson(container);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        sceneTransferData = Clone(container);
    }

    public static bool TryLoad(out UnitGrowthSaveContainer container)
    {
        container = null;

        if (!PlayerPrefs.HasKey(SaveKey))
            return false;

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        container = JsonUtility.FromJson<UnitGrowthSaveContainer>(json);
        EnsureContainerData(container);
        return container != null;
    }

    public static void SetSceneTransferData(UnitGrowthSaveData unitGrowthSaveData, PlayerPassiveGrowthData playerPassiveGrowthData)
    {
        sceneTransferData = CreateContainer(unitGrowthSaveData, playerPassiveGrowthData);
    }

    public static bool TryConsumeSceneTransferData(out UnitGrowthSaveContainer container)
    {
        container = null;

        if (sceneTransferData == null)
            return false;

        container = Clone(sceneTransferData);
        sceneTransferData = null;
        EnsureContainerData(container);
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        sceneTransferData = null;
    }

    public static UnitGrowthSaveContainer CreateContainer(UnitGrowthSaveData unitGrowthSaveData, PlayerPassiveGrowthData playerPassiveGrowthData)
    {
        UnitGrowthSaveContainer container = new()
        {
            unitGrowthSaveData = unitGrowthSaveData ?? new UnitGrowthSaveData(),
            playerPassiveGrowthData = playerPassiveGrowthData ?? new PlayerPassiveGrowthData()
        };

        return Clone(container);
    }

    public static UnitGrowthSaveContainer Clone(UnitGrowthSaveContainer source)
    {
        if (source == null)
            return new UnitGrowthSaveContainer();

        string json = JsonUtility.ToJson(source);
        UnitGrowthSaveContainer clone = JsonUtility.FromJson<UnitGrowthSaveContainer>(json);
        EnsureContainerData(clone);
        return clone;
    }

    private static void EnsureContainerData(UnitGrowthSaveContainer container)
    {
        if (container == null)
            return;

        if (container.unitGrowthSaveData == null)
            container.unitGrowthSaveData = new UnitGrowthSaveData();

        if (container.unitGrowthSaveData.units == null)
            container.unitGrowthSaveData.units = new System.Collections.Generic.List<UnitGrowthEntry>();

        if (container.playerPassiveGrowthData == null)
            container.playerPassiveGrowthData = new PlayerPassiveGrowthData();

        if (container.playerPassiveGrowthData.passives == null)
            container.playerPassiveGrowthData.passives = new System.Collections.Generic.List<PlayerPassiveGrowthEntry>();
    }
}

[Serializable]
public class UnitGrowthSaveContainer
{
    public UnitGrowthSaveData unitGrowthSaveData = new();
    public PlayerPassiveGrowthData playerPassiveGrowthData = new();
}
