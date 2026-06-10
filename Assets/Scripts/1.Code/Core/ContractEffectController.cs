using UnityEngine;

public class ContractEffectController : MonoBehaviour
{
    public static ContractEffectController Instance { get; private set; }

    public float ContractAttackPowerBonus { get; private set; }
    public float ContractAttackSpeedBonus { get; private set; }
    public float ContractMonsterMoveSpeedReduction { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetForBattle();
    }

    public static ContractEffectController EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        ContractEffectController existing = FindAnyObjectByType<ContractEffectController>();
        if (existing != null)
            return existing;

        GameObject root = new GameObject("ContractEffectController");
        return root.AddComponent<ContractEffectController>();
    }

    public void ResetForBattle()
    {
        ContractAttackPowerBonus = 0f;
        ContractAttackSpeedBonus = 0f;
        ContractMonsterMoveSpeedReduction = 0f;
        GameModifierState.SetContractModifiers(0f, 0f, 0f);
    }

    public void ApplyContract(ContractData contract, int triggerStage)
    {
        if (contract == null)
            return;

        Debug.Log($"[Contract] 계약 적용 완료: {contract.contractName} / TriggerStage: {triggerStage}");
        Debug.Log($"[Contract] 1단계에서는 개별 효과를 로그와 보유 상태까지만 연결합니다: {contract.effectType}");

        GameModifierState.SetContractModifiers(
            ContractAttackPowerBonus,
            ContractAttackSpeedBonus,
            ContractMonsterMoveSpeedReduction);
    }

    public void RemoveContract(ContractData contract)
    {
        if (contract == null)
            return;

        Debug.Log($"[Contract] 계약 제거: {contract.contractName}");
        GameModifierState.SetContractModifiers(
            ContractAttackPowerBonus,
            ContractAttackSpeedBonus,
            ContractMonsterMoveSpeedReduction);
    }
}
