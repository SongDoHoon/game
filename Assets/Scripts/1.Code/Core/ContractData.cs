using System.Collections.Generic;
using UnityEngine;

public enum ContractGrade
{
    Silver,
    Gold,
    Radiant
}

public enum ContractEffectType
{
    None,
    ClosedFront,
    GreedCollection,
    SacrificeCommandTower,
    DualFormation,
    EarlyGambit,
    LowGradeCounterattack,
    LifeHarvest,
    PerfectTenth,
    GoldenInvader,
    LuckyChainSummon,
    RiftShackle,
    GoldenAuthority,
    ForbiddenPlacement,
    GoldenJudgement,
    GreedThreshold,
    ExcessEvolution,
    GoldenOverdriveEngine,
    ReversalPawnshop,
    VaultRupture,
    WinStreakToken,
    HeavenAndHell,
    ChaosTuning,
    RiftContract,
    DivineTax,
    ForbiddenTranscendence,
    ChaosRelocation,
    FinalTwinStars,
    FinalElite,
    LifeCollateralLoan,
    SummonOverdrive,
    LifeAlchemy
}

[System.Serializable]
public class ContractData
{
    public string contractId;
    public string contractName;
    public ContractGrade contractGrade;
    public string description;
    public Sprite iconSprite;
    public List<int> availableTriggerStages = new();
    public ContractEffectType effectType;
    public bool isOwned;
    public bool isRemoved;

    public ContractData CloneRuntime()
    {
        return new ContractData
        {
            contractId = contractId,
            contractName = contractName,
            contractGrade = contractGrade,
            description = description,
            iconSprite = iconSprite,
            availableTriggerStages = availableTriggerStages != null ? new List<int>(availableTriggerStages) : new List<int>(),
            effectType = effectType,
            isOwned = isOwned,
            isRemoved = isRemoved
        };
    }

    public bool CanAppearAtStage(int triggerStage)
    {
        return availableTriggerStages != null && availableTriggerStages.Contains(triggerStage);
    }

    public string GetGradeDisplayName()
    {
        return contractGrade switch
        {
            ContractGrade.Silver => "은빛 계약",
            ContractGrade.Gold => "황금빛 계약",
            ContractGrade.Radiant => "찬란빛 계약",
            _ => "계약"
        };
    }
}
