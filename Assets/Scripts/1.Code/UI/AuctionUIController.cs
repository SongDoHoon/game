using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuctionUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject auctionPanel;

    [Header("Texts")]
    public TMP_Text leftItemText;
    public TMP_Text rightItemText;
    public TMP_Text resultText;

    [Header("Input")]
    public TMP_InputField bidInputField;

    [Header("Buttons")]
    public Button leftBidButton;
    public Button rightBidButton;

    [Header("Manager")]
    public AuctionManager auctionManager;
    public WaveManager waveManager;

    private EvolutionItemType currentLeftItem;
    private EvolutionItemType currentRightItem;
    private bool isProcessingBid;

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
        BindButtonEvents();

        currentLeftItem = leftItem;
        currentRightItem = rightItem;
        isProcessingBid = false;

        if (auctionPanel != null)
            auctionPanel.SetActive(true);

        SetBidButtonsInteractable(true);

        if (leftItemText != null)
            leftItemText.text = leftItem.ToString();

        if (rightItemText != null)
            rightItemText.text = rightItem.ToString();

        if (resultText != null)
            resultText.text = "Choose an item to bid on.";
    }

    public void CloseAuctionUI()
    {
        isProcessingBid = false;
        SetBidButtonsInteractable(false);

        if (auctionPanel != null)
            auctionPanel.SetActive(false);
    }

    public void BidLeft()
    {
        TryBid(true);
    }

    public void BidRight()
    {
        TryBid(false);
    }

    private void TryBid(bool isLeft)
    {
        if (isProcessingBid)
            return;

        if (auctionManager == null)
        {
            SetResultText("AuctionManager is not assigned");
            return;
        }

        if (bidInputField == null)
        {
            SetResultText("Bid input field is not assigned");
            return;
        }

        if (!int.TryParse(bidInputField.text, out int playerBid))
        {
            SetResultText("Enter a valid number.");
            return;
        }

        isProcessingBid = true;
        SetBidButtonsInteractable(false);

        bool result;
        int npcBid;

        if (isLeft)
            result = auctionManager.TryBidLeft(playerBid, out npcBid);
        else
            result = auctionManager.TryBidRight(playerBid, out npcBid);

        if (result)
            SetResultText($"Bid won! NPC bid: {npcBid}");
        else
            SetResultText($"Bid lost... NPC bid: {npcBid}");

        Debug.Log(resultText != null ? resultText.text : "Auction result");

        CloseAuctionUI();

        if (waveManager != null)
            waveManager.ResumeAfterAuction();
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
    }

    private void SetBidButtonsInteractable(bool interactable)
    {
        if (leftBidButton != null)
            leftBidButton.interactable = interactable;

        if (rightBidButton != null)
            rightBidButton.interactable = interactable;
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }
}
