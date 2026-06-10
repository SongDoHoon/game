using UnityEngine;

public enum AuctionBidResult
{
    Invalid,
    BidTooLow,
    NotEnoughGold,
    AIOutbid,
    PlayerWon
}

public class AuctionManager : MonoBehaviour
{
    [Header("Gold")]
    public GoldManager goldManager;

    [Header("Reward Inventory")]
    public EvolutionItemInventory itemInventory;

    private AuctionRewardOption[] currentOptions = new AuctionRewardOption[4];

    public AuctionRewardOption[] CurrentOptions => currentOptions;

    private void Awake()
    {
        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();
    }

    public void SetAuctionOptionsForStage(int stage)
    {
        currentOptions = new AuctionRewardOption[0];
        Debug.Log("[Auction] 경매 시스템 비활성화 완료: 운명의 계약 시스템을 사용합니다.");
    }

    public AuctionBidResult TryPlayerBidOption(int optionIndex, int playerBid, out int aiBid)
    {
        aiBid = 0;
        Debug.Log("[Auction] 경매 입찰 로직 비활성화 완료");
        return AuctionBidResult.Invalid;
    }

    public int GetMinimumPlayerBid(AuctionRewardOption option)
    {
        if (option == null)
            return 0;

        return option.currentPrice + 1;
    }

    private AuctionBidResult TryPlayerBid(AuctionRewardOption option, int playerBid, out int aiBid)
    {
        aiBid = 0;
        Debug.Log("[Auction] 경매 보상 적용 로직 비활성화 완료");
        return AuctionBidResult.Invalid;
    }

    private bool TryAIRebid(AuctionRewardOption option, int playerBid, out int aiBid)
    {
        aiBid = 0;
        Debug.Log("[Auction] AI 입찰 로직 비활성화 완료");
        return false;
    }

    private void ApplyReward(AuctionRewardOption option)
    {
        Debug.Log("[Auction] 경매 아이템 효과 modifier 제거 완료");
    }

}
