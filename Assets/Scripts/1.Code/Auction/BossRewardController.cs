using UnityEngine;

public class BossRewardController : MonoBehaviour
{
    public AuctionManager auctionManager;
    public AuctionUIController auctionUIController;
    public WaveManager waveManager;

    [Header("Possible Reward Pool")]
    public EvolutionItemType[] possibleItems;

    private void Awake()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
    }

    public void OpenBossAuction()
    {
        if (auctionManager == null) return;
        if (possibleItems == null || possibleItems.Length < 2) return;

        if (waveManager != null)
            waveManager.PauseForAuction();

        EvolutionItemType first = possibleItems[Random.Range(0, possibleItems.Length)];
        EvolutionItemType second = possibleItems[Random.Range(0, possibleItems.Length)];

        int safety = 0;
        while (second == first && safety < 20)
        {
            second = possibleItems[Random.Range(0, possibleItems.Length)];
            safety++;
        }

        auctionManager.SetAuctionOptions(first, second);

        if (auctionUIController != null)
            auctionUIController.OpenAuctionUI(first, second);
    }
}
