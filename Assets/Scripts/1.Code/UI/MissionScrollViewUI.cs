using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionScrollViewUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;
    public bool closeOnStart = true;

    [Header("Scroll View")]
    public Transform contentRoot;
    public MissionSlotUI missionSlotPrefab;

    [Header("Manager")]
    public MissionManager missionManager;

    private readonly List<MissionSlotUI> spawnedSlots = new();

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        BuildSlots();
        RefreshAllSlots();

        if (closeOnStart)
            ClosePanel();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (missionManager != null)
            missionManager.OnMissionStatesChanged += RefreshAllSlots;

        RefreshAllSlots();
    }

    private void OnDisable()
    {
        if (missionManager != null)
            missionManager.OnMissionStatesChanged -= RefreshAllSlots;
    }

    public void OpenPanel()
    {
        InGamePanelCoordinator.CloseOtherPanels(panelRoot);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshAllSlots();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
            return;

        if (panelRoot.activeSelf)
            ClosePanel();
        else
            OpenPanel();
    }

    public void RebuildSlots()
    {
        ClearSpawnedSlots();
        BuildSlots();
        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        ResolveReferences();

        if (missionManager == null)
            return;

        List<RuntimeMissionState> states = missionManager.GetAllMissionStates();
        EnsureSlotCount(states.Count);

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            RuntimeMissionState state = i < states.Count ? states[i] : null;
            spawnedSlots[i].Refresh(state);
        }
    }

    private void BuildSlots()
    {
        if (contentRoot == null || missionSlotPrefab == null || missionManager == null)
            return;

        List<RuntimeMissionState> states = missionManager.GetAllMissionStates();
        EnsureSlotCount(states.Count);
    }

    private void EnsureSlotCount(int targetCount)
    {
        if (contentRoot == null || missionSlotPrefab == null)
            return;

        while (spawnedSlots.Count < targetCount)
        {
            MissionSlotUI slot = Instantiate(missionSlotPrefab, contentRoot);
            spawnedSlots.Add(slot);
        }

        for (int i = 0; i < spawnedSlots.Count; i++)
            spawnedSlots[i].gameObject.SetActive(i < targetCount);
    }

    private void ClearSpawnedSlots()
    {
        for (int i = spawnedSlots.Count - 1; i >= 0; i--)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();
    }

    private void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.RemoveListener(TogglePanel);
            openButton.onClick.AddListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void ResolveReferences()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance != null
                ? MissionManager.Instance
                : FindAnyObjectByType<MissionManager>();
    }
}
