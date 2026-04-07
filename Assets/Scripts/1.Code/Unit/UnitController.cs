using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public UnitData Data { get; private set; }

    public float CurrentAttackPower { get; private set; }
    public float CurrentAttackSpeed { get; private set; }
    public float CurrentAttackRange { get; private set; }

    public int CurrentPassiveStack { get; private set; }
    public bool IsUrielMaxStackReached { get; set; }

    public UnitPlacementTile CurrentTile { get; private set; }

    private float attackTimer;
    private float activeSkillTimer;
    private MonsterController currentTarget;

    private readonly List<BuffInstance> buffs = new();

    public void Initialize(UnitData data)
    {
        Data = data;
        attackTimer = 0f;
        activeSkillTimer = 0f;
        CurrentPassiveStack = 0;
        IsUrielMaxStackReached = false;
        buffs.Clear();

        RecalculateStats();
        UnitSkillHandler.ApplyPassiveOnStart(this);
        RecalculateStats();
    }

    private void Update()
    {
        if (Data == null) return;

        UpdateBuffs();
        UpdateTarget();
        UpdateAttack();
        UpdateActiveSkill();
    }

    private void UpdateBuffs()
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            buffs[i].remainTime -= Time.deltaTime;
            if (buffs[i].remainTime <= 0f)
                buffs.RemoveAt(i);
        }

        RecalculateStats();
    }

    private void UpdateTarget()
    {
        currentTarget = UnitTargetFinder.FindNearestTarget(transform.position, CurrentAttackRange);
    }

    private void UpdateAttack()
    {
        if (currentTarget == null) return;

        attackTimer += Time.deltaTime;
        float delay = 1f / Mathf.Max(0.01f, CurrentAttackSpeed);

        if (attackTimer >= delay)
        {
            attackTimer = 0f;
            UnitAttackHandler.ExecuteBasicAttack(this, currentTarget);
        }
    }

    private void UpdateActiveSkill()
    {
        if (Data.activeSkillData == null) return;

        activeSkillTimer += Time.deltaTime;
        if (activeSkillTimer >= Data.activeSkillData.cooldown)
        {
            activeSkillTimer = 0f;
            UnitSkillHandler.ExecuteActiveSkill(this);
        }
    }

    public void RecalculateStats()
    {
        if (Data == null) return;

        CurrentAttackPower = Data.attackPower;
        CurrentAttackSpeed = Data.attackSpeed;
        CurrentAttackRange = Data.attackRange;

        foreach (BuffInstance buff in buffs)
        {
            switch (buff.buffType)
            {
                case BuffType.AttackPowerUp:
                    CurrentAttackPower += Data.attackPower * buff.value;
                    break;

                case BuffType.AttackSpeedUp:
                    CurrentAttackSpeed += Data.attackSpeed * buff.value;
                    break;

                case BuffType.RangeUp:
                    CurrentAttackRange += buff.value;
                    break;

                case BuffType.AllStatUp:
                    CurrentAttackPower += Data.attackPower * buff.value;
                    CurrentAttackSpeed += Data.attackSpeed * buff.value;
                    CurrentAttackRange += buff.value;
                    break;
            }
        }

        UnitSkillHandler.ApplyPassiveStatModifier(this);
    }

    public void AddBuff(BuffInstance buff)
    {
        if (buff == null) return;
        buffs.Add(buff);
    }

    public void AddOrRefreshRuntimeBuff(BuffType buffType, float value, float duration)
    {
        BuffInstance existing = buffs.Find(b => b.isRuntime && b.buffType == buffType && b.source == this);

        if (existing != null)
        {
            existing.value = value;
            existing.duration = duration;
            existing.remainTime = duration;
            return;
        }

        buffs.Add(new BuffInstance
        {
            buffType = buffType,
            value = value,
            duration = duration,
            remainTime = duration,
            source = this,
            isRuntime = true
        });
    }

    public void AddPassiveStack(int amount)
    {
        PassiveSkillData passive = Data.passiveSkillData;
        if (passive == null || !passive.useStack) return;

        CurrentPassiveStack += amount;
        CurrentPassiveStack = Mathf.Clamp(CurrentPassiveStack, 0, passive.maxStack);
    }

    public MonsterController GetCurrentTarget() => currentTarget;

    public int GetTargetsInRangeCount()
    {
        int count = 0;
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            if (Vector3.Distance(transform.position, monster.transform.position) <= CurrentAttackRange)
                count++;
        }

        return count;
    }

    public List<MonsterController> GetTargetsInRangeSorted()
    {
        List<MonsterController> result = new();
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            if (Vector3.Distance(transform.position, monster.transform.position) <= CurrentAttackRange)
                result.Add(monster);
        }

        result.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

        return result;
    }

    public void SetTile(UnitPlacementTile tile)
    {
        CurrentTile = tile;
    }

    public void MoveToTile(UnitPlacementTile newTile)
    {
        if (newTile == null) return;
        if (newTile.IsOccupied) return;

        if (CurrentTile != null)
            CurrentTile.ClearTile();

        newTile.PlaceExistingUnit(this);
    }
}