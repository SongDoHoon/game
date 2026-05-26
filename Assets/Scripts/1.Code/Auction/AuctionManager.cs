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
        currentOptions = GameBalanceConfig.CreateAuctionOptions(stage);
    }

    public AuctionBidResult TryPlayerBidOption(int optionIndex, int playerBid, out int aiBid)
    {
        aiBid = 0;

        if (currentOptions == null)
            return AuctionBidResult.Invalid;

        if (optionIndex < 0 || optionIndex >= currentOptions.Length)
            return AuctionBidResult.Invalid;

        return TryPlayerBid(currentOptions[optionIndex], playerBid, out aiBid);
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

        if (option == null || option.rewardType == AuctionRewardType.None)
            return AuctionBidResult.Invalid;

        if (goldManager == null)
            return AuctionBidResult.Invalid;

        if (playerBid < GetMinimumPlayerBid(option))
            return AuctionBidResult.BidTooLow;

        if (goldManager.currentGold < playerBid)
            return AuctionBidResult.NotEnoughGold;

        option.currentPrice = playerBid;
        option.hasActiveBid = true;
        option.currentBidOwner = "Player";

        if (TryAIRebid(option, playerBid, out aiBid))
        {
            option.currentPrice = aiBid;
            option.currentBidOwner = "AI";
            return AuctionBidResult.AIOutbid;
        }

        if (!goldManager.UseGold(playerBid))
            return AuctionBidResult.NotEnoughGold;

        ApplyReward(option);
        return AuctionBidResult.PlayerWon;
    }

    private bool TryAIRebid(AuctionRewardOption option, int playerBid, out int aiBid)
    {
        aiBid = 0;

        if (option == null || playerBid >= option.aiMaxBudget)
            return false;

        float burdenRate = option.aiMaxBudget > 0 ? (float)playerBid / option.aiMaxBudget : 1f;
        if (Random.value > GameBalanceConfig.GetAIRebidChance(burdenRate))
            return false;

        int firstCounterBid = Mathf.Max(option.aiFirstBid, option.GetNextAIBid(playerBid));
        int minCounterBid = playerBid + GameBalanceConfig.GetMinBidIncrease(playerBid);
        aiBid = Mathf.Max(firstCounterBid, minCounterBid);

        if (aiBid > option.aiMaxBudget)
            return false;

        return true;
    }

    private void ApplyReward(AuctionRewardOption option)
    {
        if (option.IsEvolutionItem)
        {
            if (itemInventory != null && option.rewardItemType != EvolutionItemType.None)
                itemInventory.AddItem(option.rewardItemType, 1);

            return;
        }

        GameModifierState.ApplyAuctionReward(option);
    }

}
