using UnityEngine;

public class AuctionManager : MonoBehaviour
{
    [Header("NPC Bid Range")]
    public int npcMinBid = 50;
    public int npcMaxBid = 150;

    [Header("Player Gold")]
    public int playerGold = 500;

    [Header("Reward Inventory")]
    public EvolutionItemInventory itemInventory;

    [Header("Current Auction Options")]
    public AuctionRewardOption leftOption;
    public AuctionRewardOption rightOption;

    public void SetAuctionOptions(EvolutionItemType item1, EvolutionItemType item2)
    {
        leftOption = new AuctionRewardOption
        {
            optionName = item1.ToString(),
            rewardItemType = item1
        };

        rightOption = new AuctionRewardOption
        {
            optionName = item2.ToString(),
            rewardItemType = item2
        };
    }

    public bool TryBidLeft(int playerBid, out int npcBid)
    {
        return TryBid(leftOption, playerBid, out npcBid);
    }

    public bool TryBidRight(int playerBid, out int npcBid)
    {
        return TryBid(rightOption, playerBid, out npcBid);
    }

    private bool TryBid(AuctionRewardOption option, int playerBid, out int npcBid)
    {
        npcBid = 0;

        if (option == null || option.rewardItemType == EvolutionItemType.None)
            return false;

        if (playerBid <= 0) return false;
        if (playerGold < playerBid) return false;

        playerGold -= playerBid;
        npcBid = Random.Range(npcMinBid, npcMaxBid + 1);

        Debug.Log($"플레이어 입찰가: {playerBid}, NPC 입찰가: {npcBid}");

        if (playerBid > npcBid)
        {
            if (itemInventory != null)
                itemInventory.AddItem(option.rewardItemType, 1);

            Debug.Log($"경매 성공! 획득 아이템: {option.rewardItemType}");
            return true;
        }

        Debug.Log("경매 실패!");
        return false;
    }
}