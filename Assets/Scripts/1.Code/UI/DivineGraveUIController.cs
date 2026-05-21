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
    public Sprite goldSprite;

    [Header("Magic Stone")]
    public TMP_Text currentMagicStoneText;
    public Image magicStoneIcon;
    public TMP_Text alwaysVisibleMagicStoneText;
    public Image alwaysVisibleMagicStoneIcon;
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
            currentGoldText.text = FormatCurrencyAmount("골드", currentGold, goldIcon);

        if (currentMagicStoneText != null)
            currentMagicStoneText.text = FormatCurrencyAmount("마석", Math.Floor(currentMagicStone), magicStoneIcon);

        if (alwaysVisibleMagicStoneText != null)
            alwaysVisibleMagicStoneText.text = FormatCurrencyAmount("마석", Math.Floor(currentMagicStone), alwaysVisibleMagicStoneIcon);

        if (magicStonePerSecondLabelText != null)
            magicStonePerSecondLabelText.text = "Magic Stone / sec";

        if (magicStonePerSecondValueText != null)
            magicStonePerSecondValueText.text = $"{magicStonePerSecond:0.0}/s";

        if (workerCountText != null)
            workerCountText.text = $"일꾼 {workerCount}/{maxWorkers}";

        if (hireWorkerCostText != null)
            hireWorkerCostText.text = workerCount >= maxWorkers ? "MAX" : FormatCurrencyAmount("골드", nextWorkerCost, hireWorkerCostGoldIcon);

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

    private string FormatCurrencyAmount(string currencyName, double amount, Image icon)
    {
        bool useIcon = ShouldUseIcon(icon);
        string amountText = amount.ToString("0");
        return useIcon && hideCurrencyNameWhenIconIsAssigned ? amountText : $"{currencyName} {amountText}";
    }

    private void RefreshCurrencyIcons()
    {
        goldIcon = EnsureIconImage(goldIcon, currentGoldText, "Gold Icon", goldSprite);
        magicStoneIcon = EnsureIconImage(magicStoneIcon, currentMagicStoneText, "Magic Stone Icon", magicStoneSprite);
        alwaysVisibleMagicStoneIcon = EnsureIconImage(alwaysVisibleMagicStoneIcon, alwaysVisibleMagicStoneText, "Always Visible Magic Stone Icon", magicStoneSprite);
        magicStonePerSecondIcon = EnsureIconImage(magicStonePerSecondIcon, magicStonePerSecondLabelText, "Magic Stone Per Second Icon", magicStoneSprite);
        hireWorkerCostGoldIcon = EnsureIconImage(hireWorkerCostGoldIcon, hireWorkerCostText, "Hire Worker Gold Icon", goldSprite);

        SetIconSprite(goldIcon, goldSprite);
        SetIconSprite(magicStoneIcon, magicStoneSprite);
        SetIconSprite(alwaysVisibleMagicStoneIcon, magicStoneSprite);
        SetIconSprite(magicStonePerSecondIcon, magicStoneSprite);
        SetIconSprite(hireWorkerCostGoldIcon, goldSprite);

        SetIconVisible(goldIcon, ShouldUseIcon(goldIcon));
        SetIconVisible(magicStoneIcon, ShouldUseIcon(magicStoneIcon));
        SetIconVisible(alwaysVisibleMagicStoneIcon, ShouldUseIcon(alwaysVisibleMagicStoneIcon));
        SetIconVisible(magicStonePerSecondIcon, ShouldUseIcon(magicStonePerSecondIcon));
        SetIconVisible(hireWorkerCostGoldIcon, ShouldUseIcon(hireWorkerCostGoldIcon));
    }

    private static bool ShouldUseIcon(Image icon)
    {
        return icon != null && icon.sprite != null;
    }

    private static void SetIconSprite(Image icon, Sprite sprite)
    {
        if (icon != null && sprite != null)
            icon.sprite = sprite;
    }

    private static void SetIconVisible(Image icon, bool visible)
    {
        if (icon != null)
            icon.gameObject.SetActive(visible);
    }

    private Image EnsureIconImage(Image icon, TMP_Text targetText, string iconObjectName, Sprite sprite)
    {
        if (icon != null || sprite == null || !createIconWhenSpriteIsAssigned || targetText == null)
            return icon;

        GameObject iconObject = new GameObject(iconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(targetText.transform, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = new Vector2(-iconSpacing, 0f);

        Image createdIcon = iconObject.GetComponent<Image>();
        createdIcon.raycastTarget = false;
        return createdIcon;
    }
}
