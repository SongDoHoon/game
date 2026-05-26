using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AuctionUIController : MonoBehaviour
{
    private static readonly char[] KoreanFontProbeCharacters =
    {
        '\uBC31',
        '\uD638',
        '\uCCAD',
        '\uB8E1',
        '\uD604',
        '\uBB34',
        '\uC8FC',
        '\uC791',
        '\uB3C4',
        '\uCCA0',
        '\uAD81',
        '\uAE30',
        '\uC62C',
        '\uD63C',
        '\uB3C8'
    };

    [Header("Root")]
    public GameObject auctionPanel;
    public GameObject optionSelectionPanel;
    public GameObject biddingPanel;

    [Header("Texts")]
    public TMP_Text[] optionTexts;
    public TMP_Text selectedItemText;
    public TMP_Text currentPriceText;
    public TMP_Text resultText;

    [Header("Currency Icons")]
    public Image[] optionStartPriceGoldIcons;

    [Header("Display")]
    public bool hideCurrencyNameWhenIconIsAssigned = true;

    [Header("Font")]
    public bool autoFindKoreanFontAsset = true;

    [Header("Input")]
    public TMP_InputField bidInputField;

    [Header("Buttons")]
    public Button[] optionBidButtons;
    public Button submitBidButton;
    public Button giveUpButton;

    [Header("Manager")]
    public AuctionManager auctionManager;
    public WaveManager waveManager;

    private AuctionRewardOption[] currentOptions;
    private int selectedOptionIndex = -1;
    private bool isProcessingBid;
    private bool triedAutoFindKoreanFontAsset;
    private Coroutine closeAfterResultCoroutine;
    private TMP_FontAsset auctionFontAsset;

    private void Awake()
    {
        BindButtonEvents();
        ResolveAuctionFontAsset();
        ApplyAuctionFont();

        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();
    }

    private void Start()
    {
        CloseAuctionUI();
    }

    public void OpenAuctionUI(AuctionRewardOption[] options)
    {
        BindButtonEvents();
        ApplyAuctionFont();
        UnitPlacementManager.Instance?.ClearInspectedUnit();
        InGamePanelCoordinator.CloseAllPanels();

        currentOptions = options;
        isProcessingBid = false;

        if (auctionPanel != null)
            auctionPanel.SetActive(true);

        ShowOptionSelectionPanel();
        SetOptionButtonsInteractable(true);

        RefreshOptionTexts();
        SetResultText("Choose an item.");
    }

    public void CloseAuctionUI()
    {
        isProcessingBid = false;
        selectedOptionIndex = -1;
        SetOptionButtonsInteractable(false);

        if (closeAfterResultCoroutine != null)
        {
            StopCoroutine(closeAfterResultCoroutine);
            closeAfterResultCoroutine = null;
        }

        if (auctionPanel != null)
            auctionPanel.SetActive(false);

        if (optionSelectionPanel != null)
            optionSelectionPanel.SetActive(false);

        if (biddingPanel != null)
            biddingPanel.SetActive(false);
    }

    private void BindButtonEvents()
    {
        if (submitBidButton != null)
        {
            submitBidButton.onClick.RemoveListener(SubmitSelectedBid);
            submitBidButton.onClick.AddListener(SubmitSelectedBid);
        }

        if (giveUpButton != null)
        {
            giveUpButton.onClick.RemoveListener(GiveUpBid);
            giveUpButton.onClick.AddListener(GiveUpBid);
        }

        if (optionBidButtons == null)
            return;

        for (int i = 0; i < optionBidButtons.Length; i++)
        {
            Button button = optionBidButtons[i];
            if (button == null)
                continue;

            int optionIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectOption(optionIndex));
        }
    }

    private void SetOptionButtonsInteractable(bool interactable)
    {
        if (optionBidButtons == null)
            return;

        for (int i = 0; i < optionBidButtons.Length; i++)
        {
            if (optionBidButtons[i] != null)
                optionBidButtons[i].interactable = interactable && currentOptions != null && i < currentOptions.Length;
        }
    }

    private void SelectOption(int optionIndex)
    {
        if (isProcessingBid)
            return;

        if (auctionManager == null)
        {
            SetResultText("AuctionManager is not assigned.");
            return;
        }

        if (currentOptions == null || optionIndex < 0 || optionIndex >= currentOptions.Length || currentOptions[optionIndex] == null)
            return;

        selectedOptionIndex = optionIndex;
        ShowBiddingPanel();
    }

    private void SubmitSelectedBid()
    {
        if (isProcessingBid)
            return;

        if (auctionManager == null)
        {
            SetResultText("AuctionManager is not assigned.");
            return;
        }

        if (bidInputField == null)
        {
            SetResultText("Bid input field is not assigned.");
            return;
        }

        if (selectedOptionIndex < 0)
            return;

        if (!int.TryParse(bidInputField.text, out int playerBid))
        {
            SetResultText("Enter a valid number.");
            return;
        }

        AuctionRewardOption option = currentOptions[selectedOptionIndex];
        int minimumBid = auctionManager.GetMinimumPlayerBid(option);
        if (playerBid < minimumBid)
        {
            SetResultText($"Bid must be higher than current bid. Minimum: {minimumBid}");
            return;
        }

        isProcessingBid = true;
        AuctionBidResult bidResult = auctionManager.TryPlayerBidOption(selectedOptionIndex, playerBid, out int aiBid);
        isProcessingBid = false;

        switch (bidResult)
        {
            case AuctionBidResult.AIOutbid:
                SetResultText($"AI bid {aiBid} gold.");
                RefreshBiddingPanel();
                break;

            case AuctionBidResult.PlayerWon:
                FinishAuctionWithResult("You Win");
                break;

            case AuctionBidResult.BidTooLow:
                SetResultText($"Bid must be higher than current bid. Minimum: {minimumBid}");
                break;

            case AuctionBidResult.NotEnoughGold:
                SetResultText("Not enough gold.");
                break;

            default:
                SetResultText("Auction cannot proceed.");
                break;
        }
    }

    private void GiveUpBid()
    {
        FinishAuctionWithResult("You lose");
    }

    private void FinishAuctionWithResult(string message)
    {
        SetResultText(message);
        SetOptionButtonsInteractable(false);

        if (submitBidButton != null)
            submitBidButton.interactable = false;

        if (giveUpButton != null)
            giveUpButton.interactable = false;

        if (closeAfterResultCoroutine != null)
            StopCoroutine(closeAfterResultCoroutine);

        closeAfterResultCoroutine = StartCoroutine(CoCloseAfterResult());
    }

    private IEnumerator CoCloseAfterResult()
    {
        yield return new WaitForSeconds(1f);

        CloseAuctionUI();

        if (waveManager != null)
            waveManager.ResumeAfterAuction();
    }

    private void ShowOptionSelectionPanel()
    {
        selectedOptionIndex = -1;

        if (optionSelectionPanel != null)
            optionSelectionPanel.SetActive(true);

        if (biddingPanel != null)
            biddingPanel.SetActive(false);
    }

    private void ShowBiddingPanel()
    {
        if (optionSelectionPanel != null)
            optionSelectionPanel.SetActive(false);

        if (biddingPanel != null)
            biddingPanel.SetActive(true);

        RefreshBiddingPanel();
    }

    private void RefreshBiddingPanel()
    {
        ApplyAuctionFont();

        if (currentOptions == null || selectedOptionIndex < 0 || selectedOptionIndex >= currentOptions.Length)
            return;

        AuctionRewardOption option = currentOptions[selectedOptionIndex];
        if (selectedItemText != null)
            selectedItemText.text = option.optionName;

        if (currentPriceText != null)
            currentPriceText.text = CurrencyDisplayUtility.FormatAmount("Current Bid:", option.currentPrice, null, hideCurrencyNameWhenIconIsAssigned);

        if (bidInputField != null)
            bidInputField.text = string.Empty;

        if (submitBidButton != null)
            submitBidButton.interactable = true;

        if (giveUpButton != null)
            giveUpButton.interactable = true;
    }

    private void RefreshOptionTexts()
    {
        ApplyAuctionFont();

        if (optionTexts == null)
            return;

        for (int i = 0; i < optionTexts.Length; i++)
            RefreshOptionText(optionTexts[i], i);
    }

    private void RefreshOptionText(TMP_Text text, int optionIndex)
    {
        if (text == null)
            return;

        ApplyAuctionFont(text);

        if (currentOptions == null || optionIndex < 0 || optionIndex >= currentOptions.Length || currentOptions[optionIndex] == null)
        {
            text.text = string.Empty;
            return;
        }

        AuctionRewardOption option = currentOptions[optionIndex];
        Image startPriceIcon = GetOptionStartPriceIcon(optionIndex);
        text.text = $"{option.optionName}\n{CurrencyDisplayUtility.FormatAmount("Start:", option.startPrice, startPriceIcon, hideCurrencyNameWhenIconIsAssigned)}";
        CurrencyDisplayUtility.SetIconVisible(startPriceIcon, CurrencyDisplayUtility.ShouldUseIcon(startPriceIcon));
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
        {
            ApplyAuctionFont(resultText);
            resultText.text = message;
        }
    }

    private void ApplyAuctionFont()
    {
        ResolveAuctionFontAsset();

        if (auctionFontAsset == null)
            return;

        ApplyAuctionFont(selectedItemText);
        ApplyAuctionFont(currentPriceText);
        ApplyAuctionFont(resultText);

        if (optionTexts != null)
        {
            foreach (TMP_Text optionText in optionTexts)
                ApplyAuctionFont(optionText);
        }

        if (bidInputField == null)
            return;

        ApplyAuctionFont(bidInputField.textComponent);

        if (bidInputField.placeholder is TMP_Text placeholderText)
            ApplyAuctionFont(placeholderText);
    }

    private void ApplyAuctionFont(TMP_Text targetText)
    {
        ResolveAuctionFontAsset();

        if (targetText == null || auctionFontAsset == null)
            return;

        if (targetText.font == auctionFontAsset)
            return;

        targetText.font = auctionFontAsset;
    }

    private void ResolveAuctionFontAsset()
    {
        if (auctionFontAsset != null || !autoFindKoreanFontAsset || triedAutoFindKoreanFontAsset)
            return;

        triedAutoFindKoreanFontAsset = true;
        TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset fontAsset in fontAssets)
        {
            if (fontAsset == null)
                continue;

            if (CanRenderAuctionKorean(fontAsset))
            {
                auctionFontAsset = fontAsset;
                return;
            }
        }
    }

    private static bool CanRenderAuctionKorean(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
            return false;

        foreach (char character in KoreanFontProbeCharacters)
        {
            if (fontAsset.HasCharacter(character))
                return true;
        }

        return false;
    }

    private Image GetOptionStartPriceIcon(int optionIndex)
    {
        if (optionStartPriceGoldIcons == null || optionIndex < 0 || optionIndex >= optionStartPriceGoldIcons.Length)
            return null;

        return optionStartPriceGoldIcons[optionIndex];
    }

}
