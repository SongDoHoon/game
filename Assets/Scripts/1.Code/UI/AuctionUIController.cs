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

    private EvolutionItemType currentLeftItem;
    private EvolutionItemType currentRightItem;

    private void Start()
    {
        CloseAuctionUI();
    }

    public void OpenAuctionUI(EvolutionItemType leftItem, EvolutionItemType rightItem)
    {
        currentLeftItem = leftItem;
        currentRightItem = rightItem;

        if (auctionPanel != null)
            auctionPanel.SetActive(true);

        if (leftItemText != null)
            leftItemText.text = leftItem.ToString();

        if (rightItemText != null)
            rightItemText.text = rightItem.ToString();

        if (resultText != null)
            resultText.text = "입찰할 아이템을 선택하세요.";
    }

    public void CloseAuctionUI()
    {
        if (auctionPanel != null)
            auctionPanel.SetActive(false);
    }

    public void BidLeft()
    {
        TryBid(currentLeftItem, true);
    }

    public void BidRight()
    {
        TryBid(currentRightItem, false);
    }

    private void TryBid(EvolutionItemType itemType, bool isLeft)
    {
        if (auctionManager == null)
        {
            if (resultText != null)
                resultText.text = "AuctionManager 연결 안 됨";
            return;
        }

        if (bidInputField == null)
        {
            if (resultText != null)
                resultText.text = "입찰 입력칸 연결 안 됨";
            return;
        }

        if (!int.TryParse(bidInputField.text, out int playerBid))
        {
            if (resultText != null)
                resultText.text = "숫자를 입력하세요.";
            return;
        }

        bool result;
        int npcBid;

        if (isLeft)
            result = auctionManager.TryBidLeft(playerBid, out npcBid);
        else
            result = auctionManager.TryBidRight(playerBid, out npcBid);

        if (resultText != null)
        {
            if (result)
                resultText.text = $"낙찰 성공! NPC 입찰가: {npcBid}";
            else
                resultText.text = $"낙찰 실패... NPC 입찰가: {npcBid}";
        }

        Debug.Log(resultText != null ? resultText.text : "경매 결과 출력");

        CloseAuctionUI();
    }
}