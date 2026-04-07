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
    private SpriteRenderer spriteRenderer;
    private TextMesh nameTextMesh;

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
        ApplyVisualIdentity();
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

    private void ApplyVisualIdentity()
    {
        if (Data == null)
            return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.color = GetUnitColor();

        transform.localScale = Vector3.one * GetUnitScale();

        EnsureNameText();
        RefreshNameText();
    }

    private void EnsureNameText()
    {
        if (nameTextMesh != null)
            return;

        Transform existing = transform.Find("UnitNameText");
        if (existing != null)
        {
            nameTextMesh = existing.GetComponent<TextMesh>();
            if (nameTextMesh != null)
                return;
        }

        GameObject labelObject = new GameObject("UnitNameText");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.85f, 0f);

        nameTextMesh = labelObject.AddComponent<TextMesh>();
        nameTextMesh.anchor = TextAnchor.MiddleCenter;
        nameTextMesh.alignment = TextAlignment.Center;
        nameTextMesh.fontSize = 48;
        nameTextMesh.characterSize = 0.06f;
        nameTextMesh.color = Color.white;

        MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 20;
    }

    private void RefreshNameText()
    {
        if (nameTextMesh == null || Data == null)
            return;

        nameTextMesh.text = GetDisplayName();
        nameTextMesh.color = GetLabelColor();
    }

    private string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(Data.unitName))
            return Data.unitName;

        return Data.name;
    }

    private Color GetUnitColor()
    {
        if (Data.specialLogicType == SpecialUnitLogicType.Uriel)
            return new Color(1f, 0.55f, 0.15f, 1f);

        switch (Data.grade)
        {
            case UnitGrade.Normal:
                return new Color(0.65f, 0.8f, 1f, 1f);

            case UnitGrade.Epic:
                return new Color(0.75f, 0.55f, 1f, 1f);

            case UnitGrade.Verure:
                return new Color(1f, 0.35f, 0.35f, 1f);

            case UnitGrade.ArchAngel:
                return new Color(1f, 0.84f, 0.35f, 1f);

            case UnitGrade.GreatDemon:
                return new Color(0.55f, 0.15f, 0.15f, 1f);

            default:
                return Color.white;
        }
    }

    private Color GetLabelColor()
    {
        switch (Data.grade)
        {
            case UnitGrade.Normal:
                return Color.white;

            case UnitGrade.Epic:
                return new Color(0.95f, 0.8f, 1f, 1f);

            case UnitGrade.Verure:
                return new Color(1f, 0.9f, 0.9f, 1f);

            case UnitGrade.ArchAngel:
                return new Color(1f, 0.95f, 0.7f, 1f);

            case UnitGrade.GreatDemon:
                return new Color(1f, 0.7f, 0.7f, 1f);

            default:
                return Color.white;
        }
    }

    private float GetUnitScale()
    {
        switch (Data.grade)
        {
            case UnitGrade.Normal:
                return 1f;

            case UnitGrade.Epic:
                return 1.08f;

            case UnitGrade.Verure:
                return 1.14f;

            case UnitGrade.ArchAngel:
            case UnitGrade.GreatDemon:
                return 1.22f;

            default:
                return 1f;
        }
    }
}
