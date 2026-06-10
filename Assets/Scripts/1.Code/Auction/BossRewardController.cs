using UnityEngine;

public class BossRewardController : MonoBehaviour
{
    public AuctionManager auctionManager;
    public AuctionUIController auctionUIController;
    public WaveManager waveManager;

    private void Awake()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();
    }

    public void OpenBossAuction()
    {
        int stage = waveManager != null ? waveManager.currentWave : 0;
        if (!ContractManager.IsContractTriggerStage(stage))
            return;

        if (waveManager != null)
            waveManager.PauseForAuction();

        ContractManager contractManager = ContractManager.EnsureInstance(waveManager);
        if (contractManager == null)
        {
            if (waveManager != null)
                waveManager.ResumeAfterContract();

            return;
        }

        bool offered = contractManager.TryOfferContract(stage, () =>
        {
            if (waveManager != null)
                waveManager.ResumeAfterContract();
        });

        if (!offered && waveManager != null)
            waveManager.ResumeAfterContract();
    }
}
