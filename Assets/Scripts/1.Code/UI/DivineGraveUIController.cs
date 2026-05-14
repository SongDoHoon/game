using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DivineGraveUIController : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;

    [Header("References")]
    public BattleMagicStoneManager battleMagicStoneManager;
    public GoldManager goldManager;

    [Header("Panel")]
    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;
    public Image backgroundImage;

    [Header("Gold")]
    public TMP_Text currentGoldText;
    public Image goldIcon;

    [Header("Magic Stone")]
    public TMP_Text currentMagicStoneText;
    public Image magicStoneIcon;
    public TMP_Text alwaysVisibleMagicStoneText;
    public Image alwaysVisibleMagicStoneIcon;
    public TMP_Text magicStonePerSecondLabelText;
    public Image magicStonePerSecondIcon;
    public TMP_Text magicStonePerSecondValueText;

    [Header("Worker")]
    public TMP_Text workerCountText;
    public Button hireWorkerButton;
    public TMP_Text hireWorkerCostText;
    public Image hireWorkerCostGoldIcon;

    [Header("Display")]
    public bool closeOnStart = true;

    private float refreshTimer;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        RefreshUI();

        if (closeOnStart)
            ClosePanel();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;
        RefreshUI();
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void TryHireWorker()
    {
        ResolveReferences();

        if (battleMagicStoneManager != null)
            battleMagicStoneManager.TryHireWorker();

        RefreshUI();
    }

    public void RefreshUI()
    {
        ResolveReferences();

        int currentGold = goldManager != null ? goldManager.currentGold : 0;
        double currentMagicStone = battleMagicStoneManager != null ? battleMagicStoneManager.CurrentBattleMagicStone : 0.0;
        float magicStonePerSecond = battleMagicStoneManager != null ? battleMagicStoneManager.MagicStonePerSecond : 0f;
        int workerCount = battleMagicStoneManager != null ? battleMagicStoneManager.WorkerCount : 0;
        int maxWorkers = battleMagicStoneManager != null ? battleMagicStoneManager.MaxWorkers : 30;
        int nextWorkerCost = battleMagicStoneManager != null ? battleMagicStoneManager.GetNextWorkerCost() : 0;
        bool canHireWorker = battleMagicStoneManager != null && battleMagicStoneManager.CanHireWorker();

        if (currentGoldText != null)
            currentGoldText.text = $"怨⑤뱶 {currentGold}";

        if (currentMagicStoneText != null)
            currentMagicStoneText.text = $"留덉꽍 {Math.Floor(currentMagicStone)}";

        if (alwaysVisibleMagicStoneText != null)
            alwaysVisibleMagicStoneText.text = $"留덉꽍 {Math.Floor(currentMagicStone)}";

        if (magicStonePerSecondLabelText != null)
            magicStonePerSecondLabelText.text = "Magic Stone / sec";

        if (magicStonePerSecondValueText != null)
            magicStonePerSecondValueText.text = $"{magicStonePerSecond:0.0}/s";

        if (workerCountText != null)
            workerCountText.text = $"?쇨씔 {workerCount}/{maxWorkers}";

        if (hireWorkerCostText != null)
            hireWorkerCostText.text = workerCount >= maxWorkers ? "MAX" : $"怨좎슜 {nextWorkerCost}";

        if (hireWorkerButton != null)
            hireWorkerButton.interactable = canHireWorker;
    }

    private void ResolveReferences()
    {
        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance;

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = FindAnyObjectByType<BattleMagicStoneManager>();

        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();
    }

    private void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (hireWorkerButton != null)
        {
            hireWorkerButton.onClick.RemoveListener(TryHireWorker);
            hireWorkerButton.onClick.AddListener(TryHireWorker);
        }
    }
}
