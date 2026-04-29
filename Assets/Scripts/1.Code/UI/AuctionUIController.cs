using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AuctionUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject auctionPanel;
    public GameObject optionSelectionPanel;
    public GameObject biddingPanel;

    [Header("Texts")]
    public TMP_Text leftItemText;
    public TMP_Text rightItemText;
    public TMP_Text[] optionTexts;
    public TMP_Text selectedItemText;
    public TMP_Text currentPriceText;
    public TMP_Text resultText;

    [Header("Input")]
    public TMP_InputField bidInputField;

    [Header("Buttons")]
    public Button leftBidButton;
    public Button rightBidButton;
    public Button[] optionBidButtons;
    public Button submitBidButton;
    public Button giveUpButton;

    [Header("Manager")]
    public AuctionManager auctionManager;
    public WaveManager waveManager;

    private AuctionRewardOption[] currentOptions;
    private int selectedOptionIndex = -1;
    private bool isProcessingBid;
    private Coroutine closeAfterResultCoroutine;

    private void Awake()
    {
        BindButtonEvents();

        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
    }

    private void Start()
    {
        CloseAuctionUI();
    }

    public void OpenAuctionUI(EvolutionItemType leftItem, EvolutionItemType rightItem)
    {
        OpenAuctionUI(new[]
        {
            AuctionRewardOption.CreateEvolutionItem(leftItem, 10),
            AuctionRewardOption.CreateEvolutionItem(rightItem, 10)
        });
    }

    public void OpenAuctionUI(AuctionRewardOption[] options)
    {
        BindButtonEvents();

        currentOptions = options;
        isProcessingBid = false;

        if (auctionPanel != null)
            auctionPanel.SetActive(true);

        ShowOptionSelectionPanel();
        SetOptionButtonsInteractable(true);

        RefreshOptionText(leftItemText, 0);
        RefreshOptionText(rightItemText, 1);
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

    public void BidLeft()
    {
        SelectOption(0);
    }

    public void BidRight()
    {
        SelectOption(1);
    }

    private void BindButtonEvents()
    {
        if (leftBidButton != null)
        {
            leftBidButton.onClick.RemoveListener(BidLeft);
            leftBidButton.onClick.AddListener(BidLeft);
        }

        if (rightBidButton != null)
        {
            rightBidButton.onClick.RemoveListener(BidRight);
            rightBidButton.onClick.AddListener(BidRight);
        }

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
        if (leftBidButton != null)
            leftBidButton.interactable = interactable;

        if (rightBidButton != null)
            rightBidButton.interactable = interactable;

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
        if (currentOptions == null || selectedOptionIndex < 0 || selectedOptionIndex >= currentOptions.Length)
            return;

        AuctionRewardOption option = currentOptions[selectedOptionIndex];
        if (selectedItemText != null)
            selectedItemText.text = option.optionName;

        if (currentPriceText != null)
            currentPriceText.text = $"Current Bid: {option.currentPrice}";

        if (bidInputField != null)
            bidInputField.text = string.Empty;

        if (submitBidButton != null)
            submitBidButton.interactable = true;

        if (giveUpButton != null)
            giveUpButton.interactable = true;
    }

    private void RefreshOptionTexts()
    {
        if (optionTexts == null)
            return;

        for (int i = 0; i < optionTexts.Length; i++)
            RefreshOptionText(optionTexts[i], i);
    }

    private void RefreshOptionText(TMP_Text text, int optionIndex)
    {
        if (text == null)
            return;

        if (currentOptions == null || optionIndex < 0 || optionIndex >= currentOptions.Length || currentOptions[optionIndex] == null)
        {
            text.text = string.Empty;
            return;
        }

        AuctionRewardOption option = currentOptions[optionIndex];
        text.text = $"{option.optionName}\nStart: {option.startPrice}";
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }
}
