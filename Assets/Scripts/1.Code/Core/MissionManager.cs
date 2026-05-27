using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    private const string DivineBeastMaterialId = "DivineBeast";
    private const string FourFiendMaterialId = "FourFiend";

    public static MissionManager Instance { get; private set; }

    public event Action OnMissionStatesChanged;

    [Header("Managers")]
    public UnitPlacementManager placementManager;
    public SummonManager summonManager;
    public GoldManager goldManager;
    public BattleMagicStoneManager battleMagicStoneManager;
    public EvolutionItemInventory itemInventory;
    public EvolutionManager evolutionManager;

    [Header("Runtime")]
    public List<MissionData> missionData = new();
    [SerializeField] private List<RuntimeMissionState> missionStates = new();

    private readonly List<UnitData> cachedKnownUnits = new();
    private readonly List<UnitData> fieldUnitsBuffer = new();
    private readonly List<bool> satisfiedStatesBuffer = new();
    private readonly List<bool> consumedUnitsBuffer = new();
    private readonly Dictionary<string, int> usedMaterialsBuffer = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        SubscribeMaterialInventory();

        if (missionStates.Count == 0)
            InitializeMissions();
    }

    private void OnEnable()
    {
        SubscribeMaterialInventory();
    }

    private void OnDisable()
    {
        if (itemInventory != null)
            itemInventory.OnItemCountChanged -= HandleItemCountChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitializeMissions()
    {
        ResolveReferences();
        SubscribeMaterialInventory();
        RefreshKnownUnits();
        BuildDefaultMissionData();

        missionStates.Clear();

        foreach (MissionData data in missionData)
        {
            RuntimeMissionState state = new RuntimeMissionState
            {
                missionId = data.missionId,
                missionData = data,
                isCleared = false
            };

            foreach (MissionRequirement requirement in data.requirements)
            {
                MissionRequirement resolvedRequirement = ResolveRequirement(requirement);
                state.resolvedRequirements.Add(resolvedRequirement);
                state.slotSatisfiedStates.Add(false);
            }

            missionStates.Add(state);
        }

        RefreshMissionStates();
        OnMissionStatesChanged?.Invoke();
    }

    public void RefreshMissionStates()
    {
        ResolveReferences();
        SubscribeMaterialInventory();

        bool changed = false;
        List<UnitData> fieldUnits = GetCurrentFieldUnits();

        foreach (RuntimeMissionState state in missionStates)
        {
            if (state == null || state.isCleared)
                continue;

            List<bool> nextSatisfiedStates = EvaluateMissionSlots(state, fieldUnits);
            bool stateChanged = ApplySlotStates(state, nextSatisfiedStates);
            changed |= stateChanged;

            if (AreAllSlotsSatisfied(nextSatisfiedStates))
            {
                CompleteMission(state);
                changed = true;
            }
        }

        if (changed)
            OnMissionStatesChanged?.Invoke();
    }

    public bool IsMissionCleared(string missionId)
    {
        RuntimeMissionState state = GetMissionState(missionId);
        return state != null && state.isCleared;
    }

    public RuntimeMissionState GetMissionState(string missionId)
    {
        return missionStates.Find(state => state != null && state.missionId == missionId);
    }

    public List<RuntimeMissionState> GetAllMissionStates()
    {
        return missionStates;
    }

    public void NotifyFieldUnitsChanged()
    {
        RefreshMissionStates();
    }

    public void NotifyMaterialInventoryChanged()
    {
        RefreshMissionStates();
    }

    public int GetClearedMissionCount()
    {
        int count = 0;

        foreach (RuntimeMissionState state in missionStates)
        {
            if (state != null && state.isCleared)
                count++;
        }

        return count;
    }

    private void CompleteMission(RuntimeMissionState state)
    {
        if (state == null || state.isCleared)
            return;

        state.isCleared = true;

        for (int i = 0; i < state.slotSatisfiedStates.Count; i++)
            state.slotSatisfiedStates[i] = true;

        MissionData data = state.missionData;
        if (data == null)
            return;

        ResolveReferences();

        if (goldManager != null && data.rewardGold > 0)
            goldManager.AddGold(data.rewardGold);

        if (battleMagicStoneManager != null && data.rewardBattleMagicStone > 0)
            battleMagicStoneManager.AddBattleMagicStone(data.rewardBattleMagicStone);
    }

    private List<bool> EvaluateMissionSlots(RuntimeMissionState state, List<UnitData> fieldUnits)
    {
        PrepareMissionEvaluationBuffers(state, fieldUnits);

        for (int i = 0; i < state.resolvedRequirements.Count; i++)
            satisfiedStatesBuffer.Add(false);

        for (int i = 0; i < state.resolvedRequirements.Count; i++)
        {
            MissionRequirement requirement = state.resolvedRequirements[i];
            if (requirement == null || requirement.requirementType != MissionRequirementType.SpecificUnit)
                continue;

            int unitIndex = FindAvailableSpecificUnitIndex(fieldUnits, consumedUnitsBuffer, requirement);
            if (unitIndex < 0)
                continue;

            consumedUnitsBuffer[unitIndex] = true;
            satisfiedStatesBuffer[i] = true;
        }

        for (int i = 0; i < state.resolvedRequirements.Count; i++)
        {
            MissionRequirement requirement = state.resolvedRequirements[i];
            if (requirement == null || requirement.requirementType != MissionRequirementType.AnyUnitOfGrade)
                continue;

            int unitIndex = FindAvailableGradeUnitIndex(fieldUnits, consumedUnitsBuffer, requirement.grade);
            if (unitIndex < 0)
                continue;

            consumedUnitsBuffer[unitIndex] = true;
            satisfiedStatesBuffer[i] = true;
        }

        for (int i = 0; i < state.resolvedRequirements.Count; i++)
        {
            MissionRequirement requirement = state.resolvedRequirements[i];
            if (requirement == null || requirement.requirementType != MissionRequirementType.Material)
                continue;

            string materialKey = GetMaterialKey(requirement);
            int usedCount = usedMaterialsBuffer.TryGetValue(materialKey, out int count) ? count : 0;
            if (GetOwnedMaterialCount(requirement) <= usedCount)
                continue;

            usedMaterialsBuffer[materialKey] = usedCount + 1;
            satisfiedStatesBuffer[i] = true;
        }

        return satisfiedStatesBuffer;
    }

    private void PrepareMissionEvaluationBuffers(RuntimeMissionState state, List<UnitData> fieldUnits)
    {
        satisfiedStatesBuffer.Clear();
        usedMaterialsBuffer.Clear();
        EnsureListCapacity(satisfiedStatesBuffer, state != null ? state.resolvedRequirements.Count : 0);
        PrepareConsumedUnitsBuffer(fieldUnits != null ? fieldUnits.Count : 0);
    }

    private void PrepareConsumedUnitsBuffer(int unitCount)
    {
        consumedUnitsBuffer.Clear();
        EnsureListCapacity(consumedUnitsBuffer, unitCount);

        for (int i = 0; i < unitCount; i++)
            consumedUnitsBuffer.Add(false);
    }

    private static void EnsureListCapacity<T>(List<T> list, int capacity)
    {
        if (list != null && list.Capacity < capacity)
            list.Capacity = capacity;
    }

    private bool ApplySlotStates(RuntimeMissionState state, List<bool> nextSatisfiedStates)
    {
        bool changed = false;

        for (int i = 0; i < nextSatisfiedStates.Count; i++)
        {
            if (i >= state.slotSatisfiedStates.Count)
            {
                state.slotSatisfiedStates.Add(nextSatisfiedStates[i]);
                changed = true;
                continue;
            }

            if (state.slotSatisfiedStates[i] == nextSatisfiedStates[i])
                continue;

            state.slotSatisfiedStates[i] = nextSatisfiedStates[i];
            changed = true;
        }

        return changed;
    }

    private bool AreAllSlotsSatisfied(List<bool> satisfiedStates)
    {
        if (satisfiedStates == null || satisfiedStates.Count == 0)
            return false;

        foreach (bool satisfied in satisfiedStates)
        {
            if (!satisfied)
                return false;
        }

        return true;
    }

    private int FindAvailableSpecificUnitIndex(List<UnitData> fieldUnits, List<bool> consumedUnits, MissionRequirement requirement)
    {
        for (int i = 0; i < fieldUnits.Count; i++)
        {
            if (consumedUnits[i])
                continue;

            if (IsSameUnit(fieldUnits[i], requirement))
                return i;
        }

        return -1;
    }

    private int FindAvailableGradeUnitIndex(List<UnitData> fieldUnits, List<bool> consumedUnits, UnitGrade grade)
    {
        for (int i = 0; i < fieldUnits.Count; i++)
        {
            if (consumedUnits[i])
                continue;

            UnitData unitData = fieldUnits[i];
            if (unitData != null && unitData.grade == grade)
                return i;
        }

        return -1;
    }

    private bool IsSameUnit(UnitData unitData, MissionRequirement requirement)
    {
        if (unitData == null || requirement == null)
            return false;

        if (requirement.resolvedUnitData != null)
            return unitData == requirement.resolvedUnitData;

        if (!string.IsNullOrWhiteSpace(requirement.unitId) && unitData.unitId == requirement.unitId)
            return true;

        if (!string.IsNullOrWhiteSpace(requirement.unitId) && unitData.unitName == requirement.unitId)
            return true;

        if (!string.IsNullOrWhiteSpace(requirement.displayName) && unitData.unitName == requirement.displayName)
            return true;

        string unitNameKey = NormalizeName(unitData.unitName);
        return (!string.IsNullOrWhiteSpace(requirement.unitId) && unitNameKey == NormalizeName(requirement.unitId))
            || (!string.IsNullOrWhiteSpace(requirement.displayName) && unitNameKey == NormalizeName(requirement.displayName));
    }

    private int GetOwnedMaterialCount(MissionRequirement requirement)
    {
        if (itemInventory == null || requirement == null)
            return 0;

        if (requirement.evolutionItemType != EvolutionItemType.None)
            return itemInventory.GetCount(requirement.evolutionItemType);

        if (requirement.materialId == DivineBeastMaterialId)
            return GetOwnedDivineBeastCount();

        if (requirement.materialId == FourFiendMaterialId)
            return GetOwnedFourFiendCount();

        return 0;
    }

    private int GetOwnedDivineBeastCount()
    {
        return itemInventory.GetCount(EvolutionItemType.Baekho)
            + itemInventory.GetCount(EvolutionItemType.Cheongryong)
            + itemInventory.GetCount(EvolutionItemType.Hyeonmu)
            + itemInventory.GetCount(EvolutionItemType.Jujak);
    }

    private int GetOwnedFourFiendCount()
    {
        return itemInventory.GetCount(EvolutionItemType.Taotie)
            + itemInventory.GetCount(EvolutionItemType.Qiongqi)
            + itemInventory.GetCount(EvolutionItemType.Taowu)
            + itemInventory.GetCount(EvolutionItemType.Hundun);
    }

    private string GetMaterialKey(MissionRequirement requirement)
    {
        if (requirement.evolutionItemType != EvolutionItemType.None)
            return requirement.evolutionItemType.ToString();

        return requirement.materialId;
    }

    private List<UnitData> GetCurrentFieldUnits()
    {
        fieldUnitsBuffer.Clear();

        if (placementManager == null)
            placementManager = UnitPlacementManager.Instance != null
                ? UnitPlacementManager.Instance
                : FindAnyObjectByType<UnitPlacementManager>();

        if (placementManager == null)
            return fieldUnitsBuffer;

        IReadOnlyList<UnitController> placedUnits = placementManager.GetPlacedUnits();
        foreach (UnitController unit in placedUnits)
        {
            if (unit != null && unit.Data != null)
                fieldUnitsBuffer.Add(unit.Data);
        }

        return fieldUnitsBuffer;
    }

    private MissionRequirement ResolveRequirement(MissionRequirement source)
    {
        MissionRequirement requirement = source.Clone();

        if (requirement.requirementType == MissionRequirementType.RandomGradeUnit)
        {
            UnitData randomUnit = RollRandomUnitByGrade(requirement.grade);
            requirement.requirementType = MissionRequirementType.SpecificUnit;
            requirement.resolvedUnitData = randomUnit;
            requirement.unitId = randomUnit != null ? randomUnit.unitId : requirement.unitId;
            requirement.displayName = GetUnitName(randomUnit, requirement.displayName);
            requirement.displayIcon = GetUnitIcon(randomUnit);
            return requirement;
        }

        if (requirement.requirementType == MissionRequirementType.SpecificUnit)
        {
            UnitData resolvedUnit = FindKnownUnit(requirement.unitId, requirement.displayName);
            requirement.resolvedUnitData = resolvedUnit;

            if (resolvedUnit != null)
            {
                requirement.unitId = resolvedUnit.unitId;
                requirement.displayName = GetUnitName(resolvedUnit, requirement.displayName);
                requirement.displayIcon = GetUnitIcon(resolvedUnit);
            }
        }

        return requirement;
    }

    private UnitData RollRandomUnitByGrade(UnitGrade grade)
    {
        List<UnitData> candidates = new();

        foreach (UnitData unitData in cachedKnownUnits)
        {
            if (unitData != null && unitData.grade == grade)
                candidates.Add(unitData);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private UnitData FindKnownUnit(string unitId, string displayName)
    {
        foreach (UnitData unitData in cachedKnownUnits)
        {
            if (unitData == null)
                continue;

            if (!string.IsNullOrWhiteSpace(unitId) && unitData.unitId == unitId)
                return unitData;

            if (!string.IsNullOrWhiteSpace(unitId) && unitData.unitName == unitId)
                return unitData;

            if (!string.IsNullOrWhiteSpace(displayName) && unitData.unitName == displayName)
                return unitData;

            string unitNameKey = NormalizeName(unitData.unitName);
            if (!string.IsNullOrWhiteSpace(unitId) && unitNameKey == NormalizeName(unitId))
                return unitData;

            if (!string.IsNullOrWhiteSpace(displayName) && unitNameKey == NormalizeName(displayName))
                return unitData;
        }

        return null;
    }

    private string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace(" ", string.Empty);
    }

    private void RefreshKnownUnits()
    {
        cachedKnownUnits.Clear();

        ResolveReferences();

        if (summonManager != null && summonManager.summonTable != null)
        {
            AddUnitsFromEntries(summonManager.summonTable.normalUnits);
            AddUnitsFromEntries(summonManager.summonTable.rareUnits);
            AddUnitsFromEntries(summonManager.summonTable.epicUnits);
            AddUnitsFromEntries(summonManager.summonTable.verureUnits);
        }

        UnitGrowthManager growthManager = UnitGrowthManager.Instance;
        if (growthManager == null)
            growthManager = FindAnyObjectByType<UnitGrowthManager>();

        if (growthManager != null && growthManager.unitDatabase != null)
        {
            foreach (UnitData unitData in growthManager.unitDatabase)
                AddKnownUnit(unitData);
        }

        if (evolutionManager != null && evolutionManager.recipes != null)
        {
            foreach (EvolutionRecipe recipe in evolutionManager.recipes)
            {
                if (recipe == null)
                    continue;

                AddKnownUnit(recipe.requiredBaseUnit);
                AddKnownUnit(recipe.resultUnit);
            }
        }
    }

    private void AddUnitsFromEntries(List<WeightedUnitEntry> entries)
    {
        if (entries == null)
            return;

        foreach (WeightedUnitEntry entry in entries)
        {
            if (entry != null)
                AddKnownUnit(entry.unitData);
        }
    }

    private void AddKnownUnit(UnitData unitData)
    {
        if (unitData == null || cachedKnownUnits.Contains(unitData))
            return;

        cachedKnownUnits.Add(unitData);
    }

    private Sprite GetUnitIcon(UnitData unitData)
    {
        if (unitData == null)
            return null;

        return unitData.portraitSprite != null ? unitData.portraitSprite : unitData.unitSprite;
    }

    private string GetUnitName(UnitData unitData, string fallback)
    {
        if (unitData == null)
            return fallback;

        if (!string.IsNullOrWhiteSpace(unitData.unitName))
            return unitData.unitName;

        return unitData.name;
    }

    private void ResolveReferences()
    {
        if (placementManager == null)
            placementManager = UnitPlacementManager.Instance != null
                ? UnitPlacementManager.Instance
                : FindAnyObjectByType<UnitPlacementManager>();

        if (summonManager == null)
            summonManager = FindAnyObjectByType<SummonManager>();

        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();

        if (battleMagicStoneManager == null)
            battleMagicStoneManager = BattleMagicStoneManager.Instance != null
                ? BattleMagicStoneManager.Instance
                : FindAnyObjectByType<BattleMagicStoneManager>();

        if (itemInventory == null)
            itemInventory = FindAnyObjectByType<EvolutionItemInventory>();

        if (evolutionManager == null)
            evolutionManager = FindAnyObjectByType<EvolutionManager>();
    }

    private void SubscribeMaterialInventory()
    {
        if (itemInventory == null)
            itemInventory = FindAnyObjectByType<EvolutionItemInventory>();

        if (itemInventory == null)
            return;

        itemInventory.OnItemCountChanged -= HandleItemCountChanged;
        itemInventory.OnItemCountChanged += HandleItemCountChanged;
    }

    private void HandleItemCountChanged(EvolutionItemType itemType, int count)
    {
        NotifyMaterialInventoryChanged();
    }

    private void BuildDefaultMissionData()
    {
        missionData.Clear();

        missionData.Add(new MissionData("mission_01", "동물 수집가 I", 34, 11,
            MissionRequirement.SpecificUnit("청린 도마뱀"),
            MissionRequirement.SpecificUnit("홍새"),
            MissionRequirement.SpecificUnit("백아")));

        missionData.Add(new MissionData("mission_02", "동물 수집가 II", 34, 11,
            MissionRequirement.RandomGradeUnit(UnitGrade.Normal),
            MissionRequirement.RandomGradeUnit(UnitGrade.Normal),
            MissionRequirement.RandomGradeUnit(UnitGrade.Normal)));

        missionData.Add(new MissionData("mission_03", "첫 소환 성공", 34, 11,
            MissionRequirement.RandomGradeUnit(UnitGrade.Normal),
            MissionRequirement.RandomGradeUnit(UnitGrade.Rare),
            MissionRequirement.RandomGradeUnit(UnitGrade.Epic)));

        missionData.Add(new MissionData("mission_04", "음과 양 수집가", 46, 15,
            MissionRequirement.SpecificUnit("음양 두루미"),
            MissionRequirement.SpecificUnit("균형조")));

        missionData.Add(new MissionData("mission_05", "나도 이제 고수", 46, 15,
            MissionRequirement.RandomGradeUnit(UnitGrade.Rare),
            MissionRequirement.RandomGradeUnit(UnitGrade.Normal),
            MissionRequirement.RandomGradeUnit(UnitGrade.Epic)));

        missionData.Add(new MissionData("mission_06", "불꽃이 닭", 59, 18,
            MissionRequirement.SpecificUnit("주작의 화령죠"),
            MissionRequirement.SpecificUnit("주작의 화령죠"),
            MissionRequirement.SpecificUnit("주작의 화령죠")));

        missionData.Add(new MissionData("mission_07", "성장 I", 89, 30,
            MissionRequirement.SpecificUnit("청린 도마뱀"),
            MissionRequirement.SpecificUnit("청룡의 잔린"),
            MissionRequirement.SpecificUnit("집행자")));

        missionData.Add(new MissionData("mission_08", "성장 II", 89, 30,
            MissionRequirement.SpecificUnit("백아"),
            MissionRequirement.SpecificUnit("백호의 혈수"),
            MissionRequirement.SpecificUnit("지옥의 파괴군주")));

        missionData.Add(new MissionData("mission_09", "성장 III", 89, 30,
            MissionRequirement.SpecificUnit("홍새"),
            MissionRequirement.SpecificUnit("주작의 화령죠"),
            MissionRequirement.SpecificUnit("성역의 심판자")));

        missionData.Add(new MissionData("mission_10", "성장 IV", 89, 30,
            MissionRequirement.SpecificUnit("흑각 거북"),
            MissionRequirement.SpecificUnit("현무의 수호령"),
            MissionRequirement.SpecificUnit("타락의 대마도사")));

        missionData.Add(new MissionData("mission_11", "성장 V", 118, 59,
            MissionRequirement.SpecificUnit("음양 두루미"),
            MissionRequirement.SpecificUnit("균형조"),
            MissionRequirement.SpecificUnit("균형의 집행자")));

        missionData.Add(new MissionData("mission_12", "한계에 도전하라", 118, 59,
            MissionRequirement.AnyUnitOfGrade(UnitGrade.Verure, "베르어"),
            MissionRequirement.MaterialGroup(DivineBeastMaterialId, "신수"),
            MissionRequirement.MaterialGroup(FourFiendMaterialId, "흉수")));

        missionData.Add(new MissionData("mission_13", "천사와 악마의 균형", 250, 120,
            MissionRequirement.AnyUnitOfGrade(UnitGrade.Verure, "베르어"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마")));

        missionData.Add(new MissionData("mission_14", "초고수 I", 280, 130,
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마")));

        missionData.Add(new MissionData("mission_15", "초고수 II", 280, 130,
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마")));

        missionData.Add(new MissionData("mission_16", "혼돈의 대적자 I", 300, 150,
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.ArchAngel, "대천사")));

        missionData.Add(new MissionData("mission_17", "혼돈의 대적자 II", 300, 150,
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마"),
            MissionRequirement.AnyUnitOfGrade(UnitGrade.GreatDemon, "대악마")));
    }
}
