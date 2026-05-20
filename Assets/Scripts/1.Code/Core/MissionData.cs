using System.Collections.Generic;
using UnityEngine;

public enum MissionRequirementType
{
    SpecificUnit,
    RandomGradeUnit,
    AnyUnitOfGrade,
    Material
}

[System.Serializable]
public class MissionRequirement
{
    public MissionRequirementType requirementType;
    public string unitId;
    public string materialId;
    public UnitGrade grade;
    public string displayName;
    public Sprite displayIcon;
    public UnitData resolvedUnitData;
    public EvolutionItemType evolutionItemType;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            if (resolvedUnitData != null && !string.IsNullOrWhiteSpace(resolvedUnitData.unitName))
                return resolvedUnitData.unitName;

            if (resolvedUnitData != null)
                return resolvedUnitData.name;

            if (requirementType == MissionRequirementType.Material && evolutionItemType != EvolutionItemType.None)
                return evolutionItemType.ToString();

            return grade.ToString();
        }
    }

    public Sprite DisplayIcon
    {
        get
        {
            if (displayIcon != null)
                return displayIcon;

            if (resolvedUnitData == null)
                return null;

            return resolvedUnitData.portraitSprite != null
                ? resolvedUnitData.portraitSprite
                : resolvedUnitData.unitSprite;
        }
    }

    public MissionRequirement Clone()
    {
        return new MissionRequirement
        {
            requirementType = requirementType,
            unitId = unitId,
            materialId = materialId,
            grade = grade,
            displayName = displayName,
            displayIcon = displayIcon,
            resolvedUnitData = resolvedUnitData,
            evolutionItemType = evolutionItemType
        };
    }

    public static MissionRequirement SpecificUnit(string unitNameOrId)
    {
        return new MissionRequirement
        {
            requirementType = MissionRequirementType.SpecificUnit,
            unitId = unitNameOrId,
            displayName = unitNameOrId
        };
    }

    public static MissionRequirement RandomGradeUnit(UnitGrade grade)
    {
        return new MissionRequirement
        {
            requirementType = MissionRequirementType.RandomGradeUnit,
            grade = grade,
            displayName = GetRandomGradeDisplayName(grade)
        };
    }

    public static MissionRequirement AnyUnitOfGrade(UnitGrade grade, string displayName)
    {
        return new MissionRequirement
        {
            requirementType = MissionRequirementType.AnyUnitOfGrade,
            grade = grade,
            displayName = displayName
        };
    }

    public static MissionRequirement Material(EvolutionItemType itemType, string displayName)
    {
        return new MissionRequirement
        {
            requirementType = MissionRequirementType.Material,
            materialId = itemType.ToString(),
            evolutionItemType = itemType,
            displayName = displayName
        };
    }

    public static MissionRequirement MaterialGroup(string materialId, string displayName)
    {
        return new MissionRequirement
        {
            requirementType = MissionRequirementType.Material,
            materialId = materialId,
            displayName = displayName
        };
    }

    private static string GetRandomGradeDisplayName(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.Normal => "일반(랜덤)",
            UnitGrade.Rare => "레어(랜덤)",
            UnitGrade.Epic => "에픽(랜덤)",
            _ => $"{grade}(랜덤)"
        };
    }
}

[System.Serializable]
public class MissionData
{
    public string missionId;
    public string missionName;
    public List<MissionRequirement> requirements = new();
    public int rewardGold;
    public int rewardBattleMagicStone;

    public MissionData()
    {
    }

    public MissionData(string missionId, string missionName, int rewardGold, int rewardBattleMagicStone, params MissionRequirement[] requirements)
    {
        this.missionId = missionId;
        this.missionName = missionName;
        this.rewardGold = rewardGold;
        this.rewardBattleMagicStone = rewardBattleMagicStone;
        this.requirements = requirements != null
            ? new List<MissionRequirement>(requirements)
            : new List<MissionRequirement>();
    }
}

[System.Serializable]
public class RuntimeMissionState
{
    public string missionId;
    public bool isCleared;
    public MissionData missionData;
    public List<MissionRequirement> resolvedRequirements = new();
    public List<bool> slotSatisfiedStates = new();
}
