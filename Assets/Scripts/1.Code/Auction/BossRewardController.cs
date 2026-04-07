using UnityEngine;

public class BossRewardController : MonoBehaviour
{
    public AuctionManager auctionManager;
    public AuctionUIController auctionUIController;

    [Header("Possible Reward Pool")]
    public EvolutionItemType[] possibleItems;

    public void OpenBossAuction()
    {
        if (auctionManager == null) return;
        if (possibleItems == null || possibleItems.Length < 2) return;

        EvolutionItemType first = possibleItems[Random.Range(0, possibleItems.Length)];
        EvolutionItemType second = possibleItems[Random.Range(0, possibleItems.Length)];

        int safety = 0;
        while (second == first && safety < 20)
        {
            second = possibleItems[Random.Range(0, possibleItems.Length)];
            safety++;
        }

        auctionManager.SetAuctionOptions(first, second);

        Debug.Log($"경매 시작! 선택지: {first}, {second}");

        if (auctionUIController != null)
        {
            auctionUIController.OpenAuctionUI(first, second);
        }
    }
}