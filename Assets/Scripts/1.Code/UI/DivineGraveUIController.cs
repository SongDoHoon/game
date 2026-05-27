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

    [Header("Gold")]
    public TMP_Text currentGoldText;
    public Image goldIcon;
    public Sprite goldSprite;

    [Header("Magic Stone")]
    public TMP_Text currentMagicStoneText;
    public Image magicStoneIcon;
    public TMP_Text alwaysVisibleMagicStoneText;
    public Sprite magicStoneSprite;
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
    public bool hideCurrencyNameWhenIconIsAssigned = true;
    public bool createIconWhenSpriteIsAssigned = true;
    public Vector2 iconSize = new(32f, 32f);
    public float iconSpacing = 6f;

    private float refreshTimer;
    private Image alwaysVisibleMagicStoneIcon;

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
        InGamePanelCoordinator.CloseOtherPanels(panelRoot);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshUI();
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

        RefreshCurrencyIcons();

        if (currentGoldText != null)
            SetTextIfChanged(currentGoldText, CurrencyDisplayUtility.FormatAmount("°ñµå", currentGold, goldIcon, hideCurrencyNameWhenIconIsAssigned));

        if (currentMagicStoneText != null)
            SetTextIfChanged(currentMagicStoneText, CurrencyDisplayUtility.FormatAmount("¸¶¼®", Math.Floor(currentMagicStone), magicStoneIcon, hideCurrencyNameWhenIconIsAssigned));

        if (alwaysVisibleMagicStoneText != null)
            SetTextIfChanged(alwaysVisibleMagicStoneText, CurrencyDisplayUtility.FormatAmount("¸¶¼®", Math.Floor(currentMagicStone), alwaysVisibleMagicStoneIcon, hideCurrencyNameWhenIconIsAssigned));

        if (magicStonePerSecondLabelText != null)
            SetTextIfChanged(magicStonePerSecondLabelText, "Magic Stone / sec");

        if (magicStonePerSecondValueText != null)
            SetTextIfChanged(magicStonePerSecondValueText, $"{magicStonePerSecond:0.0}/s");

        if (workerCountText != null)
            SetTextIfChanged(workerCountText, $"ÀÏ²Û {workerCount}/{maxWorkers}");

        if (hireWorkerCostText != null)
            SetTextIfChanged(hireWorkerCostText, workerCount >= maxWorkers ? "MAX" : CurrencyDisplayUtility.FormatAmount("°ñµå", nextWorkerCost, hireWorkerCostGoldIcon, hideCurrencyNameWhenIconIsAssigned));

        if (hireWorkerButton != null && hireWorkerButton.interactable != canHireWorker)
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
            openButton.onClick.RemoveListener(TogglePanel);
            openButton.onClick.AddListener(TogglePanel);
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

    private void RefreshCurrencyIcons()
    {
        goldIcon = CurrencyDisplayUtility.EnsureIconImage(goldIcon, currentGoldText, "Gold Icon", goldSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        magicStoneIcon = CurrencyDisplayUtility.EnsureIconImage(magicStoneIcon, currentMagicStoneText, "Magic Stone Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        alwaysVisibleMagicStoneIcon = CurrencyDisplayUtility.EnsureIconImage(alwaysVisibleMagicStoneIcon, alwaysVisibleMagicStoneText, "Always Visible Magic Stone Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        magicStonePerSecondIcon = CurrencyDisplayUtility.EnsureIconImage(magicStonePerSecondIcon, magicStonePerSecondLabelText, "Magic Stone Per Second Icon", magicStoneSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        hireWorkerCostGoldIcon = CurrencyDisplayUtility.EnsureIconImage(hireWorkerCostGoldIcon, hireWorkerCostText, "Hire Worker Gold Icon", goldSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);

        CurrencyDisplayUtility.SetIconSprite(goldIcon, goldSprite);
        CurrencyDisplayUtility.SetIconSprite(magicStoneIcon, magicStoneSprite);
        CurrencyDisplayUtility.SetIconSprite(alwaysVisibleMagicStoneIcon, magicStoneSprite);
        CurrencyDisplayUtility.SetIconSprite(magicStonePerSecondIcon, magicStoneSprite);
        CurrencyDisplayUtility.SetIconSprite(hireWorkerCostGoldIcon, goldSprite);

        CurrencyDisplayUtility.SetIconVisible(goldIcon, CurrencyDisplayUtility.ShouldUseIcon(goldIcon));
        CurrencyDisplayUtility.SetIconVisible(magicStoneIcon, CurrencyDisplayUtility.ShouldUseIcon(magicStoneIcon));
        CurrencyDisplayUtility.SetIconVisible(alwaysVisibleMagicStoneIcon, CurrencyDisplayUtility.ShouldUseIcon(alwaysVisibleMagicStoneIcon));
        CurrencyDisplayUtility.SetIconVisible(magicStonePerSecondIcon, CurrencyDisplayUtility.ShouldUseIcon(magicStonePerSecondIcon));
        CurrencyDisplayUtility.SetIconVisible(hireWorkerCostGoldIcon, CurrencyDisplayUtility.ShouldUseIcon(hireWorkerCostGoldIcon));
    }

    private static bool SetTextIfChanged(TMP_Text text, string value)
    {
        if (text == null || text.text == value)
            return false;

        text.text = value;
        return true;
    }
}
