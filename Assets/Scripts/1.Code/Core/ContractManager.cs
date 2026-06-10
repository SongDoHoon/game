using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ContractManager : MonoBehaviour
{
    private const float SelectionDuration = 30f;
    private const float ResumeDelay = 5f;

    public static ContractManager Instance { get; private set; }

    public event Action<IReadOnlyList<ContractData>> OnContractOffered;
    public event Action<ContractData> OnContractSelected;
    public event Action<ContractData> OnContractApplied;
    public event Action<ContractData> OnContractRemoved;

    private readonly List<ContractData> allContracts = new();
    private readonly List<ContractData> ownedContracts = new();
    private readonly HashSet<string> removedContractIds = new();
    private readonly HashSet<int> completedTriggerStages = new();
    private readonly List<ContractData> currentOptions = new();

    private ContractSelectionUI selectionUI;
    private ContractEffectController effectController;
    private WaveManager waveManager;
    private Action pendingCompleteCallback;
    private bool isOffering;
    private int currentTriggerStage;
    private float selectionRemainingTime;

    public float SelectionRemainingTime => selectionRemainingTime;
    public IReadOnlyList<ContractData> OwnedContracts => ownedContracts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildContractCatalog();
        ResolveDependencies();
    }

    private void Update()
    {
        if (!isOffering)
            return;

        selectionRemainingTime -= Time.deltaTime;
        if (selectionRemainingTime > 0f)
            return;

        AutoSelectOfferedContract();
    }

    public static ContractManager EnsureInstance(WaveManager manager)
    {
        ContractManager existing = Instance != null ? Instance : FindAnyObjectByType<ContractManager>();
        if (existing != null)
        {
            existing.waveManager = manager != null ? manager : existing.waveManager;
            existing.ResolveDependencies();
            return existing;
        }

        GameObject root = new GameObject("ContractManager");
        ContractManager created = root.AddComponent<ContractManager>();
        created.waveManager = manager;
        created.ResolveDependencies();
        return created;
    }

    public static bool IsContractTriggerStage(int stage)
    {
        return stage == 0 || stage == 20 || stage == 40 || stage == 60 || stage == 80;
    }

    public void ResetForBattle()
    {
        ownedContracts.Clear();
        removedContractIds.Clear();
        completedTriggerStages.Clear();
        currentOptions.Clear();
        isOffering = false;
        currentTriggerStage = 0;
        selectionRemainingTime = 0f;
        pendingCompleteCallback = null;

        foreach (ContractData contract in allContracts)
        {
            contract.isOwned = false;
            contract.isRemoved = false;
        }

        ResolveDependencies();
        effectController.ResetForBattle();
        selectionUI.Hide();
        Debug.Log("[Contract] 전투 계약 상태 초기화 완료");
    }

    public bool TryOfferContract(int triggerStage, Action onComplete)
    {
        if (!IsContractTriggerStage(triggerStage))
            return false;

        if (completedTriggerStages.Contains(triggerStage))
        {
            Debug.Log($"[Contract] 이미 처리된 계약 시점입니다: {triggerStage}");
            return false;
        }

        if (isOffering)
        {
            Debug.Log("[Contract] 이미 계약 선택지가 표시 중입니다.");
            return true;
        }

        ResolveDependencies();
        currentOptions.Clear();
        currentOptions.AddRange(CreateContractOptions(triggerStage, 5));

        if (currentOptions.Count == 0)
        {
            Debug.LogWarning($"[Contract] 표시 가능한 계약이 없습니다. TriggerStage: {triggerStage}");
            completedTriggerStages.Add(triggerStage);
            return false;
        }

        isOffering = true;
        currentTriggerStage = triggerStage;
        selectionRemainingTime = SelectionDuration;
        pendingCompleteCallback = onComplete;
        completedTriggerStages.Add(triggerStage);

        selectionUI.Show(currentOptions);
        OnContractOffered?.Invoke(currentOptions);

        Debug.Log($"[Contract] 계약 선택지 생성 완료. TriggerStage: {triggerStage}");
        Debug.Log($"[Contract] 선택지에 나온 계약 이름 목록: {BuildOptionNameList(currentOptions)}");
        return true;
    }

    public void SelectOfferedContract(int optionIndex)
    {
        if (!isOffering)
            return;

        if (optionIndex < 0 || optionIndex >= currentOptions.Count)
            return;

        ContractData selected = currentOptions[optionIndex];
        Debug.Log($"[Contract] 플레이어가 선택한 계약: {selected.contractName}");
        ApplySelectedContract(selected);
    }

    public void RemoveContract(ContractData contract)
    {
        if (contract == null || string.IsNullOrWhiteSpace(contract.contractId))
            return;

        if (!ownedContracts.Remove(contract))
            return;

        contract.isOwned = false;
        contract.isRemoved = true;
        removedContractIds.Add(contract.contractId);
        effectController.RemoveContract(contract);
        OnContractRemoved?.Invoke(contract);
        Debug.Log($"[Contract] 제거된 계약은 이번 판에서 다시 등장하지 않습니다: {contract.contractName}");
    }

    private void AutoSelectOfferedContract()
    {
        if (!isOffering || currentOptions.Count == 0)
            return;

        int index = UnityEngine.Random.Range(0, currentOptions.Count);
        ContractData selected = currentOptions[index];
        Debug.Log($"[Contract] 자동 선택된 계약: {selected.contractName}");
        ApplySelectedContract(selected);
    }

    private void ApplySelectedContract(ContractData selected)
    {
        if (selected == null)
            return;

        isOffering = false;
        selectionRemainingTime = 0f;
        selectionUI.Hide();

        selected.isOwned = true;
        ownedContracts.Add(selected);

        OnContractSelected?.Invoke(selected);
        effectController.ApplyContract(selected, currentTriggerStage);
        OnContractApplied?.Invoke(selected);

        currentOptions.Clear();
        StartCoroutine(CoCompleteAfterDelay());
    }

    private IEnumerator CoCompleteAfterDelay()
    {
        Debug.Log($"[Contract] 계약 적용 후 {ResumeDelay}초 뒤 다음 웨이브를 시작합니다.");
        yield return new WaitForSeconds(ResumeDelay);

        Action callback = pendingCompleteCallback;
        pendingCompleteCallback = null;
        callback?.Invoke();
    }

    private List<ContractData> CreateContractOptions(int triggerStage, int count)
    {
        List<ContractData> result = new();
        int safety = 0;

        while (result.Count < count && safety < 200)
        {
            safety++;
            ContractGrade grade = RollContractGrade(triggerStage);
            ContractData rolled = RollContractByGrade(triggerStage, grade, result);

            if (rolled == null)
                rolled = RollAnyContract(triggerStage, result);

            if (rolled == null)
                break;

            result.Add(rolled);
        }

        return result;
    }

    private ContractData RollContractByGrade(int triggerStage, ContractGrade grade, List<ContractData> currentSelection)
    {
        List<ContractData> pool = BuildAvailablePool(triggerStage, currentSelection, grade);
        return RollFromPool(pool);
    }

    private ContractData RollAnyContract(int triggerStage, List<ContractData> currentSelection)
    {
        List<ContractData> pool = BuildAvailablePool(triggerStage, currentSelection, null);
        return RollFromPool(pool);
    }

    private List<ContractData> BuildAvailablePool(int triggerStage, List<ContractData> currentSelection, ContractGrade? gradeFilter)
    {
        List<ContractData> pool = new();

        foreach (ContractData contract in allContracts)
        {
            if (contract == null)
                continue;

            if (gradeFilter.HasValue && contract.contractGrade != gradeFilter.Value)
                continue;

            if (!contract.CanAppearAtStage(triggerStage))
                continue;

            if (contract.isOwned || IsOwnedContract(contract.contractId))
            {
                Debug.Log($"[Contract] 중복 계약 제외: {contract.contractName}");
                continue;
            }

            if (contract.isRemoved || removedContractIds.Contains(contract.contractId))
            {
                Debug.Log($"[Contract] 제거된 계약 제외: {contract.contractName}");
                continue;
            }

            if (ContainsContract(currentSelection, contract.contractId))
            {
                Debug.Log($"[Contract] 이번 선택지 내 중복 제외: {contract.contractName}");
                continue;
            }

            if (contract.effectType == ContractEffectType.ChaosTuning && ownedContracts.Count < 2)
                continue;

            pool.Add(contract);
        }

        return pool;
    }

    private ContractData RollFromPool(List<ContractData> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, pool.Count);
        return pool[index].CloneRuntime();
    }

    private ContractGrade RollContractGrade(int triggerStage)
    {
        float roll = UnityEngine.Random.value;
        GetGradeChance(triggerStage, out float silver, out float gold);

        if (roll < silver)
            return ContractGrade.Silver;

        if (roll < silver + gold)
            return ContractGrade.Gold;

        return ContractGrade.Radiant;
    }

    private void GetGradeChance(int triggerStage, out float silver, out float gold)
    {
        switch (triggerStage)
        {
            case 20:
                silver = 0.45f;
                gold = 0.38f;
                return;

            case 40:
                silver = 0.35f;
                gold = 0.38f;
                return;

            case 60:
                silver = 0.25f;
                gold = 0.38f;
                return;

            case 80:
                silver = 0.15f;
                gold = 0.38f;
                return;

            default:
                silver = 0.55f;
                gold = 0.35f;
                return;
        }
    }

    private void ResolveDependencies()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();

        if (selectionUI == null)
            selectionUI = ContractSelectionUI.EnsureInstance(this);

        if (effectController == null)
            effectController = ContractEffectController.EnsureInstance();
    }

    private bool IsOwnedContract(string contractId)
    {
        return ContainsContract(ownedContracts, contractId);
    }

    private static bool ContainsContract(IEnumerable<ContractData> contracts, string contractId)
    {
        if (contracts == null || string.IsNullOrWhiteSpace(contractId))
            return false;

        foreach (ContractData contract in contracts)
        {
            if (contract != null && contract.contractId == contractId)
                return true;
        }

        return false;
    }

    private static string BuildOptionNameList(IReadOnlyList<ContractData> options)
    {
        StringBuilder builder = new();

        for (int i = 0; i < options.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");

            builder.Append(options[i] != null ? options[i].contractName : "None");
        }

        return builder.ToString();
    }

    private void BuildContractCatalog()
    {
        if (allContracts.Count > 0)
            return;

        int[] allTriggers = { 0, 20, 40, 60, 80 };
        int[] bossTriggers = { 20, 40, 60, 80 };

        AddContract("silver_closed_front", "폐쇄된 전선", ContractGrade.Silver, ContractEffectType.ClosedFront, allTriggers,
            "10스테이지 동안 유닛을 구매하거나 새로 배치할 수 없습니다.\n대신 현재 필드의 모든 아군 유닛 공격력 +40%, 공격속도 +15%.\n필드에 유닛이 없다면 Rare 유닛 1마리와 Normal 유닛 2마리를 직접 배치할 수 있습니다.");
        AddContract("silver_greed_collection", "탐욕의 징수", ContractGrade.Silver, ContractEffectType.GreedCollection, allTriggers,
            "적 처치 시 획득 골드 +20%.\n10스테이지마다 현재 필드에 배치된 아군 유닛 중 1마리가 무작위로 삭제됩니다.\n삭제 시 판매 골드는 지급하지 않습니다.");
        AddContract("silver_command_tower", "희생의 지휘탑", ContractGrade.Silver, ContractEffectType.SacrificeCommandTower, allTriggers,
            "아군 유닛 1마리를 직접 선택하여 버프 토템으로 변경합니다.\n토템은 공격하지 않고 판매와 합성이 불가능합니다.\n지정한 유닛의 등급에 따라 모든 아군 유닛에게 강화 효과를 제공합니다.");
        AddContract("silver_dual_formation", "양면의 진형", ContractGrade.Silver, ContractEffectType.DualFormation, allTriggers,
            "필드 유닛 수가 짝수이면 모든 아군 공격력 +10%.\n홀수이면 모든 아군 공격속도 +10%.\n유닛 수가 바뀔 때마다 다시 계산합니다.");
        AddContract("silver_early_gambit", "초반 승부수", ContractGrade.Silver, ContractEffectType.EarlyGambit, allTriggers,
            "20스테이지 전까지 모든 아군 공격력 +18%.\n20스테이지 이후 공격력 증가 효과가 종료되고 모든 아군 공격속도 -10%.");
        AddContract("silver_low_grade_counterattack", "낮은 등급의 반격", ContractGrade.Silver, ContractEffectType.LowGradeCounterattack, allTriggers,
            "필드에 Normal 유닛이 정확히 3마리 있으면 모든 아군 공격력 +35%.\nNormal 유닛 수가 바뀔 때마다 다시 계산합니다.");
        AddContract("silver_life_harvest", "생명의 수확", ContractGrade.Silver, ContractEffectType.LifeHarvest, allTriggers,
            "적 유닛을 50마리 처치할 때마다 목숨 +1.\n현재 목숨이 최대치라면 골드 100을 획득합니다.");
        AddContract("silver_perfect_tenth", "완벽한 열 번째", ContractGrade.Silver, ContractEffectType.PerfectTenth, allTriggers,
            "유닛을 10번 구매할 때마다 10번째 유닛은 Rare 이상 등급으로 등장합니다.\n10번째 유닛 구매 비용은 30% 증가합니다.");
        AddContract("silver_golden_invader", "황금빛 침입자", ContractGrade.Silver, ContractEffectType.GoldenInvader, allTriggers,
            "10스테이지마다 황금 몬스터 1마리가 일반 웨이브에 섞여 등장합니다.\n처치하면 골드 100을 얻고, 포탈에 도달하면 골드 30을 잃습니다.");
        AddContract("silver_lucky_chain_summon", "행운의 연속 소환", ContractGrade.Silver, ContractEffectType.LuckyChainSummon, allTriggers,
            "유닛 구매/소환 시 Rare 이상 유닛이 연속 2회 등장하면 다음 유닛 구매 비용이 0이 됩니다.\n진화/합성/계약 보상 유닛은 카운트하지 않습니다.");

        AddContract("gold_rift_shackle", "균열의 족쇄", ContractGrade.Gold, ContractEffectType.RiftShackle, allTriggers,
            "10초마다 포탈 근처에 감속 장판을 생성합니다.\n장판 위 적은 5초 동안 이동속도 -30%.");
        AddContract("gold_authority", "황금의 권능", ContractGrade.Gold, ContractEffectType.GoldenAuthority, allTriggers,
            "보유 골드 100마다 모든 아군 공격력 +5%.\n골드 변화 시 즉시 다시 계산합니다.");
        AddContract("gold_forbidden_placement", "금지된 배치", ContractGrade.Gold, ContractEffectType.ForbiddenPlacement, bossTriggers,
            "10스테이지 동안 유닛을 구매하거나 새로 배치할 수 없습니다.\n대신 현재 배치된 모든 아군 공격력 +80%, 공격속도 +25%.\n10스테이지를 버티면 골드 500과 마석 200을 획득합니다.");
        AddContract("gold_judgement", "황금의 심판", ContractGrade.Gold, ContractEffectType.GoldenJudgement, allTriggers,
            "아군 유닛이 공격할 때마다 1% 확률로 발동합니다.\n현재 보유 골드 x 10 피해를 추가로 입힌 뒤 현재 골드의 10%를 감소시킵니다.");
        AddContract("gold_greed_threshold", "탐욕의 임계점", ContractGrade.Gold, ContractEffectType.GreedThreshold, allTriggers,
            "보유 골드가 500 미만이면 공격력 -50%.\n500 이상 1000 미만이면 공격력 +70%.\n1000 이상이면 공격력 +100%, 공격속도 +25%.");
        AddContract("gold_excess_evolution", "과잉 진화", ContractGrade.Gold, ContractEffectType.ExcessEvolution, allTriggers,
            "Normal/Rare/Epic 유닛이 직접 처치 수 조건을 달성하면 다음 등급으로 승급합니다.\n각 유닛별 킬 카운트를 사용합니다.");
        AddContract("gold_overdrive_engine", "황금 폭주 기관", ContractGrade.Gold, ContractEffectType.GoldenOverdriveEngine, allTriggers,
            "한 스테이지에서 사용한 골드가 300 이상이면 해당 스테이지 동안 공격속도 +30%.\n다음 스테이지 동안 공격속도 -20%.");
        AddContract("gold_reversal_pawnshop", "역전의 전당포", ContractGrade.Gold, ContractEffectType.ReversalPawnshop, allTriggers,
            "보스 처치 후 목숨 2개를 소모하여 무작위 Verure 유닛 1마리를 획득할 수 있습니다.\n한 판에 최대 2회 사용할 수 있습니다.");
        AddContract("gold_vault_rupture", "금고 파열", ContractGrade.Gold, ContractEffectType.VaultRupture, allTriggers,
            "보유 골드가 정확히 1000에 도달하면 현재 골드가 0이 되고 모든 아군 공격력이 영구적으로 +250% 증가합니다.\n한 판에 최대 2회 발동합니다.");
        AddContract("gold_win_streak_token", "연승의 증표", ContractGrade.Gold, ContractEffectType.WinStreakToken, allTriggers,
            "목숨을 잃지 않고 스테이지를 클리어할 때마다 연승 스택 +1.\n스택 1개당 모든 아군 공격력 +12%, 최대 10스택.");

        AddContract("radiant_heaven_and_hell", "천국과 지옥", ContractGrade.Radiant, ContractEffectType.HeavenAndHell, allTriggers,
            "Angel 또는 Demon 계열 중 하나가 무작위로 결정됩니다.\n결정된 계열의 최고 등급 유닛 1마리와 제작 재료 유닛을 함께 획득합니다.");
        AddContract("radiant_chaos_tuning", "혼돈의 조율", ContractGrade.Radiant, ContractEffectType.ChaosTuning, allTriggers,
            "현재 보유 중인 계약 2개를 직접 선택하여 제거합니다.\n이후 무작위 찬란빛 계약 3개 중 2개를 선택하여 획득합니다.");
        AddContract("radiant_rift_contract", "균열의 계약서", ContractGrade.Radiant, ContractEffectType.RiftContract, allTriggers,
            "지금부터 모든 보스 처치 보상이 2배가 됩니다.\n대신 보스 처치에 실패하면 즉시 패배합니다.");
        AddContract("radiant_divine_tax", "신의 세금", ContractGrade.Radiant, ContractEffectType.DivineTax, allTriggers,
            "계약 획득 후 10스테이지마다 현재 보유 골드의 20%를 잃습니다.\n골드를 잃을 때마다 모든 아군 공격력이 영구적으로 +70% 증가합니다.");
        AddContract("radiant_forbidden_transcendence", "금지된 초월", ContractGrade.Radiant, ContractEffectType.ForbiddenTranscendence, allTriggers,
            "현재 필드의 가장 높은 등급 유닛 1마리를 직접 선택하여 즉시 한 단계 강화합니다.\nAngel, Demon은 제외합니다.");
        AddContract("radiant_chaos_relocation", "혼돈의 재배치", ContractGrade.Radiant, ContractEffectType.ChaosRelocation, allTriggers,
            "현재 필드의 Normal, Rare, Epic 유닛이 각각 한 단계 높은 등급의 무작위 유닛으로 변경됩니다.\nVerure, Angel, Demon은 제외합니다.");
        AddContract("radiant_final_twin_stars", "최후의 쌍성", ContractGrade.Radiant, ContractEffectType.FinalTwinStars, allTriggers,
            "플레이어의 현재 목숨을 1로 조정합니다.\n이후 Angel 또는 Demon 유닛 중 무작위 3마리를 획득합니다.");
        AddContract("radiant_final_elite", "최후의 정예", ContractGrade.Radiant, ContractEffectType.FinalElite, allTriggers,
            "아군 유닛의 최대 배치 가능 수가 절반으로 감소합니다.\n대신 배치된 모든 아군 공격력 +150%, 공격속도 +40%, 액티브 스킬 쿨타임 -25%.");
        AddContract("radiant_life_collateral_loan", "생명 담보 대출", ContractGrade.Radiant, ContractEffectType.LifeCollateralLoan, allTriggers,
            "보유 골드가 부족해도 마이너스 골드로 유닛 구매와 강화를 진행할 수 있습니다.\n다음 계약 단계에서 골드가 0 미만이면 목숨 1개를 소모합니다.");
        AddContract("radiant_summon_overdrive", "소환 폭주", ContractGrade.Radiant, ContractEffectType.SummonOverdrive, allTriggers,
            "계약 체결 후 15초 동안 모든 유닛 구매 비용이 0이 됩니다.\n효과 종료 후 10스테이지 동안 유닛 구매 비용이 50% 증가합니다.");
        AddContract("radiant_life_alchemy", "생명의 연성", ContractGrade.Radiant, ContractEffectType.LifeAlchemy, allTriggers,
            "합성에 필요한 재료가 부족할 때 부족한 재료를 목숨으로 대체할 수 있습니다.\n합성 조건 자체는 유지됩니다.");
    }

    private void AddContract(string id, string name, ContractGrade grade, ContractEffectType effectType, int[] triggerStages, string description)
    {
        allContracts.Add(new ContractData
        {
            contractId = id,
            contractName = name,
            contractGrade = grade,
            description = description,
            iconSprite = null,
            availableTriggerStages = new List<int>(triggerStages),
            effectType = effectType,
            isOwned = false,
            isRemoved = false
        });
    }
}
