using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    private const int CircleSegmentCount = 64;
    private const float DefaultCritDamageMultiplier = 2f;

    private static Material sharedCircleRendererMaterial;

    public UnitData Data { get; private set; }

    public double CurrentAttackPower { get; private set; }
    public float CurrentAttackSpeed { get; private set; }
    public float CurrentAttackInterval { get; private set; }
    public float CurrentAttackRange { get; private set; }
    public float CurrentCritChance { get; private set; }
    public float CurrentCritDamageMultiplier { get; private set; }

    public UnitPlacementTile CurrentTile { get; private set; }

    [Header("Runtime Stat Debug")]
    [SerializeField] private string debugUnitId;
    [SerializeField] private double debugCurrentAttackPower;
    [SerializeField] private float debugCurrentAttackInterval;
    [SerializeField] private float debugCurrentAttackSpeed;
    [SerializeField] private float debugCurrentAttackRange;
    [SerializeField] private float debugCurrentCritChance;
    [SerializeField] private float debugCurrentCritDamageMultiplier;
    [SerializeField] private float debugSkillCooldownTimer;
    [SerializeField] private int debugSkillBasicAttackCount;
    [SerializeField] private int debugPassiveStack;
    [SerializeField] private int debugManualEnhanceStack;
    [SerializeField] private int debugManualEnhanceCost;
    [SerializeField] private float debugManualEnhanceChance;

    private float attackTimer;
    private float skillCooldownTimer;
    private float skillAttackLockTimer;
    private float activeSelfSkillBuffTimer;
    private int skillBasicAttackCount;
    private int passiveStack;
    private int manualEnhanceStack;
    private int manualEnhanceSuccessCount;
    private bool passiveMaxStackBuffActive;
    private MonsterController currentTarget;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Sprite defaultSprite;
    private RuntimeAnimatorController defaultAnimatorController;
    private bool defaultSpriteRendererEnabled = true;
    private bool defaultVisualCached;
    private TextMesh nameTextMesh;
    private LineRenderer attackRangeRenderer;
    private LineRenderer splashRangeRenderer;
    private UnitSpineAnimationController spineAnimationController;
    private bool selectionVisualActive;

    private readonly List<BuffInstance> buffs = new();

    public void Initialize(UnitData data)
    {
        Data = data;
        attackTimer = 0f;
        skillCooldownTimer = 0f;
        skillAttackLockTimer = 0f;
        activeSelfSkillBuffTimer = 0f;
        skillBasicAttackCount = 0;
        passiveStack = 0;
        manualEnhanceStack = 0;
        manualEnhanceSuccessCount = 0;
        passiveMaxStackBuffActive = false;
        buffs.Clear();

        RecalculateStats();
        SkillData skill = GetSkillData();
        if (skill != null && skill.startWithCooldown)
            StartSkillCooldown(UnitSkillHandler.CalculateFinalCooldown(this, skill));

        UnitSkillHandler.ApplyPassiveOnStart(this);
        ApplyVisualIdentity();
    }

    private void Update()
    {
        if (Data == null) return;

        UpdateBuffs();
        TickSkillAttackLock(Time.deltaTime);
        TickActiveSelfSkillBuff(Time.deltaTime);
        UpdateTarget();
        UnitSkillHandler.UpdateContinuousEffects(this);
        UpdateAttack();
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
        currentTarget = UnitTargetFinder.FindTarget(
            transform.position,
            CurrentAttackRange,
            Data.targetPriority,
            Data.bossFallbackTargetPriority);
    }

    private void UpdateAttack()
    {
        float delay = Mathf.Max(UnitGrowthBalanceConfig.MinimumAttackInterval, CurrentAttackInterval);
        attackTimer = Mathf.Min(delay, attackTimer + Time.deltaTime);

        if (currentTarget == null) return;
        if (IsBasicAttackLockedBySkill()) return;

        if (attackTimer >= delay)
        {
            attackTimer = 0f;
            UnitAttackHandler.ExecuteBasicAttack(this, currentTarget);
            UnitSkillHandler.OnBasicAttack(this, currentTarget);
        }
    }

    public void RecalculateStats()
    {
        if (Data == null) return;

        double runtimeAttackBonus = 0.0;
        float runtimeAttackSpeedBonus = 0f;
        float runtimeCritChanceBonus = 0f;
        float runtimeCritDamageBonus = 0f;
        CurrentAttackRange = Data.attackRange;
        CurrentCritChance = 0f;
        CurrentCritDamageMultiplier = DefaultCritDamageMultiplier;

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

                case BuffType.CritChanceUp:
                    runtimeCritChanceBonus += buff.value;
                    break;

                case BuffType.CritDamageUp:
                    runtimeCritDamageBonus += buff.value;
                    break;

                case BuffType.AllStatUp:
                    runtimeAttackBonus += buff.value;
                    runtimeAttackSpeedBonus += buff.value;
                    CurrentAttackRange += buff.value;
                    break;
            }
        }

        SkillData skill = GetSkillData();
        if (skill != null)
        {
            runtimeAttackBonus += GetManualEnhanceAttackPowerBonus(skill);
            runtimeAttackSpeedBonus += GetManualEnhanceAttackSpeedBonus(skill);

            if (IsActiveSelfSkillBuffActive() && skill.activeSelfRangeOverride > 0f)
                CurrentAttackRange = Mathf.Max(CurrentAttackRange, skill.activeSelfRangeOverride);

            runtimeAttackBonus += GetActiveSelfSkillAttackPowerBonus(skill);
        }

        UnitGrowthManager growthManager = UnitGrowthManager.Instance;
        UnitGrowthEntry unitGrowth = growthManager != null ? growthManager.GetUnitGrowth(Data.unitId) : null;
        PlayerPassiveGrowthData playerPassiveGrowth = growthManager != null ? growthManager.playerPassiveGrowthData : null;
        runtimeAttackBonus += UnitSkillHandler.GetGlobalPassiveAttackPowerBonus();
        runtimeAttackSpeedBonus += UnitSkillHandler.GetGlobalPassiveAttackSpeedBonus();
        runtimeAttackBonus += UnitSkillHandler.GetSelfPassiveAttackPowerBonus(this);
        runtimeAttackSpeedBonus += UnitSkillHandler.GetSelfPassiveAttackSpeedBonus(this);
        runtimeAttackBonus += UnitSkillHandler.GetPassiveStackAttackPowerBonus(this);
        runtimeAttackSpeedBonus += UnitSkillHandler.GetPassiveStackAttackSpeedBonus(this);

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
        CurrentCritChance = Mathf.Clamp01(runtimeCritChanceBonus);
        CurrentCritDamageMultiplier = Mathf.Max(1f, DefaultCritDamageMultiplier + runtimeCritDamageBonus);
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
            AddOrRefreshRuntimeBuff(buffType, value, duration, source);
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

    public void AddOrRefreshRuntimeBuff(BuffType buffType, float value, float duration, UnitController source = null)
    {
        UnitController safeSource = source != null ? source : this;
        BuffInstance existing = buffs.Find(b => b.isRuntime && b.buffType == buffType && b.source == safeSource);

        if (existing != null)
        {
            if (value > existing.value)
            {
                existing.value = value;
                existing.duration = duration;
                existing.remainTime = duration;
            }
            else if (Mathf.Approximately(value, existing.value))
            {
                existing.duration = duration;
                existing.remainTime = duration;
            }

            return;
        }

        buffs.Add(new BuffInstance
        {
            buffType = buffType,
            value = value,
            duration = duration,
            remainTime = duration,
            source = safeSource,
            isRuntime = true
        });
    }

    public void AddPassiveStack(int amount)
    {
        passiveStack = Mathf.Max(0, passiveStack + amount);

        SkillData skill = GetSkillData();
        if (skill != null && skill.maxPassiveStack > 0)
            passiveStack = Mathf.Min(passiveStack, skill.maxPassiveStack);

        RefreshRuntimeStatDebugFields();
    }

    public void ResetPassiveStack()
    {
        passiveStack = 0;
        RefreshRuntimeStatDebugFields();
    }

    public void SetPassiveStack(int stack)
    {
        passiveStack = Mathf.Max(0, stack);

        SkillData skill = GetSkillData();
        if (skill != null && skill.maxPassiveStack > 0)
            passiveStack = Mathf.Min(passiveStack, skill.maxPassiveStack);

        RefreshRuntimeStatDebugFields();
    }

    public bool IsPassiveMaxStackBuffActive()
    {
        return passiveMaxStackBuffActive;
    }

    public void SetPassiveMaxStackBuffActive(bool active)
    {
        passiveMaxStackBuffActive = active;
    }

    public bool HasManualSelfEnhancement()
    {
        SkillData skill = GetSkillData();
        return skill != null && skill.isEnabled && skill.hasManualSelfEnhancement;
    }

    public int GetManualEnhanceStack()
    {
        return manualEnhanceStack;
    }

    public int GetManualEnhanceMaxStack()
    {
        SkillData skill = GetSkillData();
        return skill != null ? Mathf.Max(0, skill.manualEnhanceMaxStack) : 0;
    }

    public int GetManualEnhanceCost()
    {
        SkillData skill = GetSkillData();
        if (skill == null)
            return 0;

        return Mathf.Max(0, skill.manualEnhanceBaseGoldCost + manualEnhanceSuccessCount * skill.manualEnhanceGoldCostIncrease);
    }

    public float GetManualEnhanceSuccessChance()
    {
        SkillData skill = GetSkillData();
        if (skill == null)
            return 0f;

        float chance = Mathf.Clamp01(skill.manualEnhanceBaseSuccessChance);
        float multiplier = Mathf.Clamp01(skill.manualEnhanceSuccessChanceMultiplierPerSuccess);
        return Mathf.Clamp01(chance * Mathf.Pow(multiplier, manualEnhanceSuccessCount));
    }

    public bool CanTryManualEnhance(GoldManager goldManager)
    {
        if (!HasManualSelfEnhancement())
            return false;

        int maxStack = GetManualEnhanceMaxStack();
        if (maxStack > 0 && manualEnhanceStack >= maxStack)
            return false;

        return goldManager != null && goldManager.currentGold >= GetManualEnhanceCost();
    }

    public ManualEnhanceResult TryManualEnhance(GoldManager goldManager)
    {
        if (!HasManualSelfEnhancement())
            return ManualEnhanceResult.NotAvailable;

        int maxStack = GetManualEnhanceMaxStack();
        if (maxStack > 0 && manualEnhanceStack >= maxStack)
            return ManualEnhanceResult.MaxStack;

        int cost = GetManualEnhanceCost();
        if (goldManager == null || !goldManager.UseGold(cost))
            return ManualEnhanceResult.NotEnoughGold;

        bool success = Random.value <= GetManualEnhanceSuccessChance();
        if (!success)
        {
            RefreshRuntimeStatDebugFields();
            return ManualEnhanceResult.Failed;
        }

        manualEnhanceStack++;
        manualEnhanceSuccessCount++;
        RecalculateStats();
        return ManualEnhanceResult.Success;
    }

    public void StartActiveSelfSkillBuff(float duration)
    {
        activeSelfSkillBuffTimer = Mathf.Max(activeSelfSkillBuffTimer, Mathf.Max(0f, duration));
        RecalculateStats();
    }

    public bool IsActiveSelfSkillBuffActive()
    {
        return activeSelfSkillBuffTimer > 0f;
    }

    public MonsterController GetCurrentTarget() => currentTarget;

    public SkillData GetSkillData()
    {
        return Data != null ? Data.skillData : null;
    }

    public void PlayBasicAttackAnimation(MonsterController target)
    {
        if (spineAnimationController != null)
            spineAnimationController.PlayBasicAttack(target);
    }

    public void PlaySkillAnimation(MonsterController target)
    {
        if (spineAnimationController != null)
            spineAnimationController.PlaySkill(target);
    }

    public void TickSkillCooldown(float deltaTime)
    {
        if (skillCooldownTimer <= 0f)
            return;

        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - Mathf.Max(0f, deltaTime));
        RefreshRuntimeStatDebugFields();
    }

    public bool IsSkillCooldownReady()
    {
        return skillCooldownTimer <= 0f;
    }

    public void StartSkillCooldown(float cooldown)
    {
        skillCooldownTimer = Mathf.Max(0f, cooldown);
        RefreshRuntimeStatDebugFields();
    }

    public void StartSkillAttackLock(float duration)
    {
        skillAttackLockTimer = Mathf.Max(skillAttackLockTimer, Mathf.Max(0f, duration));
    }

    public bool IsBasicAttackLockedBySkill()
    {
        return skillAttackLockTimer > 0f;
    }

    private void TickSkillAttackLock(float deltaTime)
    {
        if (skillAttackLockTimer <= 0f)
            return;

        skillAttackLockTimer = Mathf.Max(0f, skillAttackLockTimer - Mathf.Max(0f, deltaTime));
    }

    private void TickActiveSelfSkillBuff(float deltaTime)
    {
        if (activeSelfSkillBuffTimer <= 0f)
            return;

        activeSelfSkillBuffTimer = Mathf.Max(0f, activeSelfSkillBuffTimer - Mathf.Max(0f, deltaTime));
        RecalculateStats();
    }

    public void AddSkillBasicAttackCount(int amount)
    {
        skillBasicAttackCount = Mathf.Max(0, skillBasicAttackCount + amount);
        RefreshRuntimeStatDebugFields();
    }

    public void ResetSkillBasicAttackCount()
    {
        skillBasicAttackCount = 0;
        RefreshRuntimeStatDebugFields();
    }

    public int GetSkillBasicAttackCount()
    {
        return skillBasicAttackCount;
    }

    public int GetPassiveStack()
    {
        return passiveStack;
    }

    public int GetNearbyEnemyCount(float radius)
    {
        int count = 0;
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

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
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

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
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

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
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);

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

        EnsureVisualComponents();

        if (spriteRenderer != null)
        {
            if (Data.unitSprite != null)
            {
                spriteRenderer.sprite = Data.unitSprite;
                spriteRenderer.color = Data.useGradeTintOnSprite ? GetUnitColor() : Color.white;
            }
            else
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = GetUnitColor();
            }
        }

        ApplyAnimatorController();
        ApplySpineVisual();

        transform.localScale = Vector3.one * GetUnitScale();

        EnsureNameText();
        RefreshNameText();
    }

    private void ApplyAnimatorController()
    {
        EnsureVisualComponents();

        if (animator != null)
            animator.runtimeAnimatorController = Data.animatorController != null ? Data.animatorController : defaultAnimatorController;
    }

    private void ApplySpineVisual()
    {
        if (Data != null && Data.spineSkeletonData != null)
        {
            if (spineAnimationController == null)
                spineAnimationController = UnitSpineAnimationController.GetOrCreate(this);

            if (spineAnimationController != null)
                spineAnimationController.Configure(this, Data);

            if (spriteRenderer != null && Data.hideSpriteWhenSpineVisual)
                spriteRenderer.enabled = false;

            return;
        }

        if (spineAnimationController != null)
            spineAnimationController.Configure(this, Data);

        if (spriteRenderer != null)
            spriteRenderer.enabled = defaultSpriteRendererEnabled;
    }

    private void EnsureVisualComponents()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (defaultVisualCached)
            return;

        defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        defaultSpriteRendererEnabled = spriteRenderer == null || spriteRenderer.enabled;
        defaultAnimatorController = animator != null ? animator.runtimeAnimatorController : null;
        defaultVisualCached = true;
    }

    private void RefreshRuntimeStatDebugFields()
    {
        debugUnitId = Data != null ? Data.unitId : string.Empty;
        debugCurrentAttackPower = CurrentAttackPower;
        debugCurrentAttackInterval = CurrentAttackInterval;
        debugCurrentAttackSpeed = CurrentAttackSpeed;
        debugCurrentAttackRange = CurrentAttackRange;
        debugCurrentCritChance = CurrentCritChance;
        debugCurrentCritDamageMultiplier = CurrentCritDamageMultiplier;
        debugSkillCooldownTimer = skillCooldownTimer;
        debugSkillBasicAttackCount = skillBasicAttackCount;
        debugPassiveStack = passiveStack;
        debugManualEnhanceStack = manualEnhanceStack;
        debugManualEnhanceCost = GetManualEnhanceCost();
        debugManualEnhanceChance = GetManualEnhanceSuccessChance();
    }

    private float GetManualEnhanceAttackPowerBonus(SkillData skill)
    {
        if (skill == null || !skill.hasManualSelfEnhancement)
            return 0f;

        return manualEnhanceStack * skill.manualEnhanceAttackPowerBonusPerStack;
    }

    private float GetManualEnhanceAttackSpeedBonus(SkillData skill)
    {
        if (skill == null || !skill.hasManualSelfEnhancement)
            return 0f;

        return manualEnhanceStack * skill.manualEnhanceAttackSpeedBonusPerStack;
    }

    private float GetActiveSelfSkillAttackPowerBonus(SkillData skill)
    {
        if (skill == null || !IsActiveSelfSkillBuffActive())
            return 0f;

        float bonus = Mathf.Max(0f, skill.activeSelfAttackPowerBonus);
        if (skill.activeAttackPowerBonusPerEnemyInRange > 0f)
        {
            float enemyBonus = GetTargetsInRangeCount() * skill.activeAttackPowerBonusPerEnemyInRange;
            if (skill.activeAttackPowerBonusMax > 0f)
                enemyBonus = Mathf.Min(enemyBonus, skill.activeAttackPowerBonusMax);

            bonus += enemyBonus;
        }

        return bonus;
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
            lineRenderer.sharedMaterial = GetSharedCircleRendererMaterial();

        lineRenderer.enabled = false;
        return lineRenderer;
    }

    private static Material GetSharedCircleRendererMaterial()
    {
        if (sharedCircleRendererMaterial != null)
            return sharedCircleRendererMaterial;

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader == null)
            return null;

        sharedCircleRendererMaterial = new Material(spriteShader)
        {
            name = "UnitRangeCircleSharedMaterial"
        };

        return sharedCircleRendererMaterial;
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

