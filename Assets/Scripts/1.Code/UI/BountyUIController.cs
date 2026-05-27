using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BountyUIController : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;

    [Header("References")]
    public BountyManager bountyManager;
    public WaveManager waveManager;
    public MonsterSpawner monsterSpawner;

    [Header("Panel")]
    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;
    public bool closeOnStart = true;

    [Header("Difficulty Buttons")]
    public Button difficulty1Button;
    public Button difficulty2Button;
    public Button difficulty3Button;
    public Button difficulty4Button;
    public Button difficulty5Button;
    public Button difficulty6Button;

    [Header("Bounty Data Texts")]
    public TMP_Text[] hpTexts = new TMP_Text[6];
    public TMP_Text[] rewardTexts = new TMP_Text[6];

    [Header("Reward Icons")]
    public Sprite goldSprite;
    public Sprite magicStoneSprite;
    public bool createRewardIconsWhenSpriteIsAssigned = true;
    public Vector2 rewardIconSize = new(28f, 28f);
    public float rewardIconTextPadding = 4f;

    [Header("Status")]
    public TMP_Text cooldownText;

    private float refreshTimer;
    private readonly Image[] runtimeRewardGoldIcons = new Image[6];
    private readonly Image[] runtimeRewardMagicStoneIcons = new Image[6];
    private string lastCooldownText;

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

    public void SpawnDifficulty1()
    {
        TrySpawnDifficulty(1);
    }

    public void SpawnDifficulty2()
    {
        TrySpawnDifficulty(2);
    }

    public void SpawnDifficulty3()
    {
        TrySpawnDifficulty(3);
    }

    public void SpawnDifficulty4()
    {
        TrySpawnDifficulty(4);
    }

    public void SpawnDifficulty5()
    {
        TrySpawnDifficulty(5);
    }

    public void SpawnDifficulty6()
    {
        TrySpawnDifficulty(6);
    }

    public bool TrySpawnDifficulty(int difficulty)
    {
        ResolveReferences();

        bool spawned = bountyManager != null && bountyManager.TrySpawnBounty(difficulty);
        RefreshUI();
        return spawned;
    }

    public void RefreshUI()
    {
        if (panelRoot != null && !panelRoot.activeInHierarchy)
            return;

        ResolveReferences();

        RefreshDifficultyButton(1, difficulty1Button);
        RefreshDifficultyButton(2, difficulty2Button);
        RefreshDifficultyButton(3, difficulty3Button);
        RefreshDifficultyButton(4, difficulty4Button);
        RefreshDifficultyButton(5, difficulty5Button);
        RefreshDifficultyButton(6, difficulty6Button);

        if (cooldownText != null)
            SetTextIfChanged(cooldownText, ref lastCooldownText, bountyManager != null
                ? $"Cooldown {Math.Ceiling(bountyManager.GetRemainingBountyCooldown()):0}s"
                : "Cooldown -");
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

        BindDifficultyButton(difficulty1Button, SpawnDifficulty1);
        BindDifficultyButton(difficulty2Button, SpawnDifficulty2);
        BindDifficultyButton(difficulty3Button, SpawnDifficulty3);
        BindDifficultyButton(difficulty4Button, SpawnDifficulty4);
        BindDifficultyButton(difficulty5Button, SpawnDifficulty5);
        BindDifficultyButton(difficulty6Button, SpawnDifficulty6);
    }

    private void BindDifficultyButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RefreshDifficultyButton(int difficulty, Button button)
    {
        BountyEliteData data = bountyManager != null ? bountyManager.GetBountyData(difficulty) : null;

        if (button != null)
            button.interactable = bountyManager != null && bountyManager.CanSpawnBounty(difficulty);

        RefreshBountyDataTexts(difficulty, data);
    }

    private void RefreshBountyDataTexts(int difficulty, BountyEliteData data)
    {
        int index = difficulty - 1;

        TMP_Text hpText = GetArrayElement(hpTexts, index);
        TMP_Text rewardText = GetArrayElement(rewardTexts, index);
        Image rewardGoldIcon = EnsureRewardIconImage(runtimeRewardGoldIcons, index, rewardText, "Gold Reward Icon", goldSprite);
        Image rewardMagicStoneIcon = EnsureRewardIconImage(runtimeRewardMagicStoneIcons, index, rewardText, "Magic Stone Reward Icon", magicStoneSprite);

        CurrencyDisplayUtility.SetIconSprite(rewardGoldIcon, goldSprite);
        CurrencyDisplayUtility.SetIconSprite(rewardMagicStoneIcon, magicStoneSprite);
        CurrencyDisplayUtility.SetIconVisible(rewardGoldIcon, data != null && data.rewardGold > 0 && CurrencyDisplayUtility.ShouldUseIcon(rewardGoldIcon));
        CurrencyDisplayUtility.SetIconVisible(rewardMagicStoneIcon, data != null && data.rewardBattleMagicStone > 0 && CurrencyDisplayUtility.ShouldUseIcon(rewardMagicStoneIcon));

        if (data == null)
        {
            if (hpText != null)
                SetTextIfChanged(hpText, "HP -");

            if (rewardText != null)
                SetTextIfChanged(rewardText, "Reward -");

            return;
        }

        if (hpText != null)
            SetTextIfChanged(hpText, $"HP {data.hp:0}");

        if (rewardText != null)
        {
            bool rewardTextChanged = SetTextIfChanged(rewardText, FormatRewardText(data));
            if (rewardTextChanged)
                PositionRewardIcons(rewardText, data, rewardGoldIcon, rewardMagicStoneIcon);
        }
    }

    private string FormatRewardText(BountyEliteData data)
    {
        bool hasGold = data.rewardGold > 0;
        bool hasMagicStone = data.rewardBattleMagicStone > 0;

        if (hasGold && hasMagicStone)
            return $"    {data.rewardGold} /     {data.rewardBattleMagicStone}";

        if (hasGold)
            return $"    {data.rewardGold}";

        if (hasMagicStone)
            return $"    {data.rewardBattleMagicStone}";

        return "\uBCF4\uC0C1 \uC5C6\uC74C";
    }

    private void ResolveReferences()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();

        if (monsterSpawner == null)
            monsterSpawner = FindAnyObjectByType<MonsterSpawner>();

        if (bountyManager == null)
            bountyManager = BountyManager.Instance;

        if (bountyManager == null)
            bountyManager = FindAnyObjectByType<BountyManager>();

        if (bountyManager == null && waveManager != null)
            bountyManager = BountyManager.EnsureInstance(waveManager, monsterSpawner);
    }

    private static T GetArrayElement<T>(T[] array, int index) where T : class
    {
        if (array == null || index < 0 || index >= array.Length)
            return null;

        return array[index];
    }

    private Image EnsureRewardIconImage(Image[] iconArray, int index, TMP_Text targetText, string iconObjectName, Sprite sprite)
    {
        if (iconArray == null || index < 0 || index >= iconArray.Length)
            return null;

        if (iconArray[index] != null)
            return iconArray[index];

        if (!createRewardIconsWhenSpriteIsAssigned || sprite == null || targetText == null)
            return null;

        Image createdIcon = CurrencyDisplayUtility.EnsureIconImage(
            null,
            targetText,
            $"{iconObjectName} {index + 1}",
            sprite,
            createRewardIconsWhenSpriteIsAssigned,
            rewardIconSize,
            new Vector2(0.5f, 0.5f),
            Vector2.zero);

        iconArray[index] = createdIcon;
        return createdIcon;
    }

    private void PositionRewardIcons(TMP_Text rewardText, BountyEliteData data, Image goldIcon, Image magicStoneIcon)
    {
        if (rewardText == null || data == null)
            return;

        rewardText.ForceMeshUpdate();

        if (data.rewardGold > 0)
            PositionIconBeforeText(goldIcon, rewardText, data.rewardGold.ToString());

        if (data.rewardBattleMagicStone > 0)
            PositionIconBeforeText(magicStoneIcon, rewardText, data.rewardBattleMagicStone.ToString(), data.rewardGold > 0);
    }

    private void PositionIconBeforeText(Image icon, TMP_Text rewardText, string targetValue, bool searchFromLast = false)
    {
        if (icon == null || rewardText == null || string.IsNullOrEmpty(targetValue))
            return;

        int startIndex = searchFromLast
            ? rewardText.text.LastIndexOf(targetValue, StringComparison.Ordinal)
            : rewardText.text.IndexOf(targetValue, StringComparison.Ordinal);

        if (startIndex < 0 || startIndex >= rewardText.textInfo.characterCount)
            return;

        TMP_CharacterInfo characterInfo = rewardText.textInfo.characterInfo[startIndex];
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        if (iconRect == null)
            return;

        float iconX = characterInfo.bottomLeft.x - rewardIconTextPadding - (rewardIconSize.x * 0.5f);
        float iconY = (characterInfo.bottomLeft.y + characterInfo.topLeft.y) * 0.5f;
        iconRect.localPosition = new Vector3(iconX, iconY, 0f);
    }

    private static bool SetTextIfChanged(TMP_Text text, string value)
    {
        if (text == null || text.text == value)
            return false;

        text.text = value;
        return true;
    }

    private static bool SetTextIfChanged(TMP_Text text, ref string cachedValue, string value)
    {
        if (cachedValue == value)
            return false;

        cachedValue = value;
        return SetTextIfChanged(text, value);
    }
}
