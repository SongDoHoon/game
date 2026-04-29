using UnityEngine;

[System.Serializable]
public class AuctionRewardOption
{
    public string optionName;
    public AuctionRewardType rewardType;
    public EvolutionItemType rewardItemType;
    public int stage;
    public int startPrice;
    public int currentPrice;
    public int aiFirstBid;
    public int aiMaxBudget;
    public AuctionAIPersonality aiPersonality;
    public bool hasActiveBid;
    public string currentBidOwner;

    public bool IsEvolutionItem => rewardType == AuctionRewardType.EvolutionItem;

    public static AuctionRewardOption CreateReward(AuctionRewardType rewardType, int stage)
    {
        AuctionRewardOption option = new()
        {
            optionName = GetRewardName(rewardType),
            rewardType = rewardType,
            rewardItemType = EvolutionItemType.None,
            stage = stage,
            startPrice = GameBalanceConfig.GetAuctionStartPrice(stage, rewardType)
        };

        option.InitializeAI();
        return option;
    }

    public static AuctionRewardOption CreateEvolutionItem(EvolutionItemType itemType, int stage)
    {
        AuctionRewardOption option = new()
        {
            optionName = itemType.ToString(),
            rewardType = AuctionRewardType.EvolutionItem,
            rewardItemType = itemType,
            stage = stage,
            startPrice = GameBalanceConfig.GetAuctionStartPrice(stage, AuctionRewardType.EvolutionItem)
        };

        option.InitializeAI();
        return option;
    }

    public void InitializeAI()
    {
        currentPrice = startPrice;
        aiFirstBid = Mathf.RoundToInt(startPrice * Random.Range(0.9f, 1.1f));
        aiPersonality = GameBalanceConfig.RollAIPersonality(stage);
        aiMaxBudget = Mathf.RoundToInt(startPrice * GameBalanceConfig.GetAIBudgetMultiplier(aiPersonality));
        hasActiveBid = false;
        currentBidOwner = string.Empty;
    }

    public int GetNextAIBid(int currentBid)
    {
        int minIncrease = GameBalanceConfig.GetMinBidIncrease(currentBid);
        int randomIncrease = Mathf.RoundToInt(currentBid * Random.Range(0.08f, 0.16f));
        return currentBid + Mathf.Max(minIncrease, randomIncrease);
    }

    private static string GetRewardName(AuctionRewardType rewardType)
    {
        return rewardType switch
        {
            AuctionRewardType.GlobalAttackSpeedUp => "All Attack Speed +5%",
            AuctionRewardType.GlobalAttackPowerUp => "All Attack Power +10%",
            AuctionRewardType.AngelDemonCooldownReduction => "Angel/Demon Cooldown -5%",
            AuctionRewardType.MonsterMoveSpeedReduction => "Monster Move Speed -5%",
            AuctionRewardType.AngelDemonSkillDamageUp => "Angel/Demon Skill Damage +5%",
            AuctionRewardType.StageStartBonusGold => "Stage Start Gold +10",
            AuctionRewardType.HigherGradeSummonChanceUp => "High Grade Summon Chance +2%",
            AuctionRewardType.MergeTwoGradeUpChance => "Merge Two Grade Up +3%",
            AuctionRewardType.UnitExchangeCostReduction => "Unit Exchange Cost -3%",
            _ => "None"
        };
    }
}
