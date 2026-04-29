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

        int stage = waveManager != null ? waveManager.currentWave : 0;
        if (!GameBalanceConfig.HasAuctionAtStage(stage))
            return;

        if (waveManager != null)
            waveManager.PauseForAuction();

        auctionManager.SetAuctionOptionsForStage(stage);

        if (auctionUIController != null)
            auctionUIController.OpenAuctionUI(auctionManager.currentOptions);
    }
}
