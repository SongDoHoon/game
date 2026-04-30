using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    private const int CircleSegmentCount = 64;

    public UnitData Data { get; private set; }

    public double CurrentAttackPower { get; private set; }
    public float CurrentAttackSpeed { get; private set; }
    public float CurrentAttackInterval { get; private set; }
    public float CurrentAttackRange { get; private set; }

    public int CurrentPassiveStack { get; private set; }
    public bool IsUrielMaxStackReached { get; set; }

    public UnitPlacementTile CurrentTile { get; private set; }

    [Header("Runtime Stat Debug")]
    [SerializeField] private string debugUnitId;
    [SerializeField] private double debugCurrentAttackPower;
    [SerializeField] private float debugCurrentAttackInterval;
    [SerializeField] private float debugCurrentAttackSpeed;
    [SerializeField] private float debugCurrentAttackRange;

    private float attackTimer;
    private float activeSkillTimer;
    private MonsterController currentTarget;
    private SpriteRenderer spriteRenderer;
    private TextMesh nameTextMesh;
    private LineRenderer attackRangeRenderer;
    private LineRenderer splashRangeRenderer;
    private bool selectionVisualActive;

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
        UnitSkillHandler.UpdateContinuousEffects(this);
        UpdateSelectionVisual();
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
        float delay = Mathf.Max(UnitGrowthBalanceConfig.MinimumAttackInterval, CurrentAttackInterval);

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
        float cooldown = Data.activeSkillData.cooldown;
        if (GameModifierState.IsEvolutionGrade(Data))
            cooldown *= Mathf.Clamp01(1f - GameModifierState.AngelDemonCooldownReduction);

        UnitGrowthManager growthManager = UnitGrowthManager.Instance;
        if (growthManager != null)
            cooldown *= Mathf.Clamp01(1f - growthManager.GetSkillCooldownReduction(Data.unitId));

        if (activeSkillTimer >= cooldown)
        {
            activeSkillTimer = 0f;
            UnitSkillHandler.ExecuteActiveSkill(this);
        }
    }

    public void RecalculateStats()
    {
        if (Data == null) return;

        double runtimeAttackBonus = 0.0;
        float runtimeAttackSpeedBonus = 0f;
        CurrentAttackRange = Data.attackRange;

        foreach (BuffInstance buff in buffs)
        {
            switch (buff.buffType)
            {
                case BuffType.AttackPowerUp:
                    runtimeAttackBonus += buff.value;
                    break;

                case BuffType.AttackSpeedUp:
                    runtimeAttackSpeedBonus += buff.value;
                    break;

                case BuffType.RangeUp:
                    CurrentAttackRange += buff.value;
                    break;

                case BuffType.AllStatUp:
                    runtimeAttackBonus += buff.value;
                    runtimeAttackSpeedBonus += buff.value;
                    CurrentAttackRange += buff.value;
                    break;
            }
        }

        UnitGrowthManager growthManager = UnitGrowthManager.Instance;
        UnitGrowthEntry unitGrowth = growthManager != null ? growthManager.GetUnitGrowth(Data.unitId) : null;
        PlayerPassiveGrowthData playerPassiveGrowth = growthManager != null ? growthManager.playerPassiveGrowthData : null;

        UnitStatModifierResult statResult = UnitStatCalculator.Calculate(
            Data,
            unitGrowth,
            playerPassiveGrowth,
            GameModifierState.GlobalAttackPowerBonus,
            GameModifierState.GlobalAttackSpeedBonus,
            runtimeAttackBonus,
            runtimeAttackSpeedBonus);

        CurrentAttackPower = statResult.finalAttack;
        CurrentAttackInterval = statResult.finalAttackInterval;
        CurrentAttackSpeed = 1f / Mathf.Max(UnitGrowthBalanceConfig.MinimumAttackInterval, CurrentAttackInterval);
        RefreshRuntimeStatDebugFields();

        UnitSkillHandler.ApplyPassiveStatModifier(this);
        RefreshRuntimeStatDebugFields();
    }

    public void AddBuff(BuffInstance buff)
    {
        if (buff == null) return;
        buffs.Add(buff);
    }

    public void ApplyExtendedBuff(BuffType buffType, float value, float duration, UnitController source, bool isRuntime)
    {
        if (isRuntime)
        {
            AddOrRefreshRuntimeBuff(buffType, value, duration);
            return;
        }

        buffs.Add(new BuffInstance
        {
            buffType = buffType,
            value = value,
            duration = duration,
            remainTime = duration,
            source = source,
            isRuntime = false
        });
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

        if (passive.maxStack > 0)
            CurrentPassiveStack = Mathf.Clamp(CurrentPassiveStack, 0, passive.maxStack);
        else
            CurrentPassiveStack = Mathf.Max(0, CurrentPassiveStack);
    }

    public MonsterController GetCurrentTarget() => currentTarget;

    public int GetNearbyEnemyCount(float radius)
    {
        int count = 0;
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            if (Vector3.Distance(transform.position, monster.transform.position) <= radius)
                count++;
        }

        return count;
    }

    public int CountEnemiesWithDebuffs(params DebuffType[] debuffTypes)
    {
        if (debuffTypes == null || debuffTypes.Length == 0)
            return 0;

        int count = 0;
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            foreach (DebuffType debuffType in debuffTypes)
            {
                if (monster.HasDebuff(debuffType))
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }

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

    private void RefreshRuntimeStatDebugFields()
    {
        debugUnitId = Data != null ? Data.unitId : string.Empty;
        debugCurrentAttackPower = CurrentAttackPower;
        debugCurrentAttackInterval = CurrentAttackInterval;
        debugCurrentAttackSpeed = CurrentAttackSpeed;
        debugCurrentAttackRange = CurrentAttackRange;
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

            case UnitGrade.Rare:
                return new Color(0.35f, 0.9f, 0.55f, 1f);

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

            case UnitGrade.Rare:
                return new Color(0.88f, 1f, 0.9f, 1f);

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
        return 0.5f;
    }

    public void SetSelectionVisualActive(bool active)
    {
        selectionVisualActive = active;

        if (!active)
        {
            SetLineRendererVisible(GetAttackRangeRenderer(), false);
            SetLineRendererVisible(GetSplashRangeRenderer(), false);
            return;
        }

        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        if (!selectionVisualActive)
            return;

        float attackRange = Application.isPlaying && Data != null ? CurrentAttackRange : GetPreviewAttackRange();
        UpdateCircleRenderer(GetAttackRangeRenderer(), transform.position, attackRange, new Color(0.2f, 0.85f, 1f, 0.95f));

        float splashRadius = GetPreviewSplashRadius();
        Vector3? splashCenter = GetSplashPreviewCenter();
        if (splashRadius > 0f && splashCenter.HasValue)
            UpdateCircleRenderer(GetSplashRangeRenderer(), splashCenter.Value, splashRadius, new Color(1f, 0.6f, 0.2f, 0.9f));
        else
            SetLineRendererVisible(GetSplashRangeRenderer(), false);
    }

    private float GetPreviewAttackRange()
    {
        return Data != null ? Data.attackRange : 0f;
    }

    private float GetPreviewSplashRadius()
    {
        if (Data == null)
            return 0f;

        switch (Data.basicAttackType)
        {
            case BasicAttackType.AoEMelee:
            case BasicAttackType.AoERanged:
                return Data.attackRadius;

            default:
                return 0f;
        }
    }

    private Vector3? GetSplashPreviewCenter()
    {
        if (Data == null)
            return null;

        switch (Data.basicAttackType)
        {
            case BasicAttackType.AoEMelee:
            case BasicAttackType.AoERanged:
                if (Application.isPlaying && currentTarget != null && currentTarget.IsAlive)
                    return currentTarget.transform.position;

                return null;

            default:
                return null;
        }
    }

    private LineRenderer GetAttackRangeRenderer()
    {
        if (attackRangeRenderer == null)
            attackRangeRenderer = CreateCircleRenderer("AttackRangeRenderer", 0.05f);

        return attackRangeRenderer;
    }

    private LineRenderer GetSplashRangeRenderer()
    {
        if (splashRangeRenderer == null)
            splashRangeRenderer = CreateCircleRenderer("SplashRangeRenderer", 0.04f);

        return splashRangeRenderer;
    }

    private LineRenderer CreateCircleRenderer(string objectName, float width)
    {
        Transform existing = transform.Find(objectName);
        LineRenderer lineRenderer = existing != null ? existing.GetComponent<LineRenderer>() : null;

        if (lineRenderer == null)
        {
            GameObject rendererObject = existing != null ? existing.gameObject : new GameObject(objectName);
            rendererObject.transform.SetParent(transform, false);
            rendererObject.transform.localPosition = Vector3.zero;

            lineRenderer = rendererObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = rendererObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = CircleSegmentCount;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.sortingOrder = 50;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
                lineRenderer.material = new Material(spriteShader);
        }

        lineRenderer.enabled = false;
        return lineRenderer;
    }

    private void UpdateCircleRenderer(LineRenderer lineRenderer, Vector3 center, float radius, Color color)
    {
        if (lineRenderer == null || radius <= 0f)
            return;

        SetLineRendererVisible(lineRenderer, true);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        float angleStep = Mathf.PI * 2f / CircleSegmentCount;
        for (int i = 0; i < CircleSegmentCount; i++)
        {
            float angle = angleStep * i;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            lineRenderer.SetPosition(i, point);
        }
    }

    private void SetLineRendererVisible(LineRenderer lineRenderer, bool visible)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }
}

