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

    [Header("Current Auction Options")]
    public AuctionRewardOption[] currentOptions = new AuctionRewardOption[4];
    public AuctionRewardOption leftOption;
    public AuctionRewardOption rightOption;

    private void Awake()
    {
        if (goldManager == null)
            goldManager = FindFirstObjectByType<GoldManager>();
    }

    public void SetAuctionOptions(EvolutionItemType item1, EvolutionItemType item2)
    {
        currentOptions = new[]
        {
            AuctionRewardOption.CreateEvolutionItem(item1, 10),
            AuctionRewardOption.CreateEvolutionItem(item2, 10)
        };

        SyncLegacyOptions();
    }

    public void SetAuctionOptionsForStage(int stage)
    {
        currentOptions = GameBalanceConfig.CreateAuctionOptions(stage);
        SyncLegacyOptions();
    }

    public bool TryBidLeft(int playerBid, out int npcBid)
    {
        return TryBid(leftOption, playerBid, out npcBid);
    }

    public bool TryBidRight(int playerBid, out int npcBid)
    {
        return TryBid(rightOption, playerBid, out npcBid);
    }

    public bool TryBidOption(int optionIndex, int playerBid, out int npcBid)
    {
        npcBid = 0;

        if (currentOptions == null)
            return false;

        if (optionIndex < 0 || optionIndex >= currentOptions.Length)
            return false;

        return TryBid(currentOptions[optionIndex], playerBid, out npcBid);
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

    private bool TryBid(AuctionRewardOption option, int playerBid, out int npcBid)
    {
        npcBid = 0;

        if (option == null || option.rewardType == AuctionRewardType.None)
            return false;

        if (goldManager == null)
            return false;

        if (playerBid < option.startPrice)
            return false;

        npcBid = ResolveAIBid(option, playerBid);

        if (playerBid <= npcBid)
            return false;

        if (!goldManager.UseGold(playerBid))
            return false;

        ApplyReward(option);
        return true;
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

    private int ResolveAIBid(AuctionRewardOption option, int playerBid)
    {
        int npcBid = Mathf.Max(option.aiFirstBid, option.startPrice);
        float burdenRate = option.aiMaxBudget > 0 ? (float)npcBid / option.aiMaxBudget : 1f;

        while (npcBid < playerBid && Random.value <= GameBalanceConfig.GetAIRebidChance(burdenRate))
        {
            int nextBid = option.GetNextAIBid(npcBid);

            if (nextBid > option.aiMaxBudget)
                break;

            npcBid = nextBid;
            burdenRate = option.aiMaxBudget > 0 ? (float)npcBid / option.aiMaxBudget : 1f;
        }

        option.currentPrice = Mathf.Max(option.currentPrice, npcBid);
        return npcBid;
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

    private void SyncLegacyOptions()
    {
        leftOption = currentOptions != null && currentOptions.Length > 0 ? currentOptions[0] : null;
        rightOption = currentOptions != null && currentOptions.Length > 1 ? currentOptions[1] : null;
    }
}
