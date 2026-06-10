using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterWalkAnimationSet
{
    public int startWave = 1;
    public int endWave = 10;
    public RuntimeAnimatorController animatorController;
}

public class MonsterController : MonoBehaviour
{
    public event Action<MonsterController, double> OnDamageTaken;
    public event Action<MonsterController, double, double> OnHpChanged;

    [Header("Info")]
    public MonsterType monsterType = MonsterType.Normal;

    [Header("Stats")]
    public double maxHp = 100.0;
    public double currentHp = 100.0;
    public float moveSpeed = 2f;
    public int rewardGold = 10;
    public bool isBoss = false;
    public int bountyDifficulty = 0;

    [Header("Path")]
    public bool destroyOnGoal = true;

    [Header("Animation")]
    public MonsterWalkAnimationSet[] walkAnimationSets = new MonsterWalkAnimationSet[10];

    [Header("Facing")]
    public bool faceMovementDirection = true;
    public bool spriteFacesRightByDefault = true;
    public float horizontalFacingThreshold = 0.001f;

    [Header("Runtime Move Speed Debug")]
    [SerializeField] private float debugSpeedMultiplier = 1f;
    [SerializeField] private float debugGlobalSpeedReduction = 0f;
    [SerializeField] private float debugGlobalSpeedMultiplier = 1f;
    [SerializeField] private float debugFinalMoveSpeed = 0f;

    private int currentWaypointIndex;
    private readonly List<DebuffInstance> debuffs = new();
    private float speedMultiplier = 1f;
    private bool isStunned;
    private WaypointPath waypointPath;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private WaveManager waveManager;
    private BountyManager bountyManager;
    private Vector3 lastPosition;
    private Vector3 frameMovementDelta;

    public bool IsAlive => currentHp > 0.0;
    public double CurrentHp => currentHp;
    public double MaxHp => maxHp;
    public bool IsBountyElite => monsterType == MonsterType.BountyElite;

    private void Start()
    {
        EnsureWalkAnimationSets();
        currentHp = maxHp;
        NotifyHpChanged();
        CacheVisualComponents();

        if (waypointPath != null)
        {
            InitializePath();
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!IsAlive) return;

        UpdateDebuffs();
        MoveAlongPath();
        frameMovementDelta = transform.position - lastPosition;
        UpdateFacingDirection();
        lastPosition = transform.position;
    }

    public void SetPath(WaypointPath path)
    {
        waypointPath = path;
        InitializePath();
    }

    public void SetWaveManager(WaveManager manager)
    {
        waveManager = manager;
    }

    public void SetBountyManager(BountyManager manager)
    {
        bountyManager = manager;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureWalkAnimationSets();
    }
#endif

    public void SetAppearanceForWave(int wave)
    {
        MonsterWalkAnimationSet animationSet = GetWalkAnimationSetForWave(wave);
        CacheVisualComponents();

        if (animator != null && animationSet != null && animationSet.animatorController != null)
            animator.runtimeAnimatorController = animationSet.animatorController;
    }

    private void InitializePath()
    {
        if (waypointPath == null) return;
        if (waypointPath.Count <= 0) return;

        Transform first = waypointPath.GetWaypoint(0);
        if (first != null)
        {
            transform.position = first.position;
            currentWaypointIndex = 1;
        }
    }

    private void MoveAlongPath()
    {
        if (waypointPath == null) return;
        if (isStunned) return;
        if (waveManager != null && waveManager.isPausedForAuction) return;

        Transform target = waypointPath.GetWaypoint(currentWaypointIndex);
        if (target == null) return;

        float globalSpeedReduction = GameModifierState.ContractMonsterMoveSpeedReduction
            + UnitSkillHandler.GetGlobalPassiveMonsterMoveSpeedReduction();
        float globalSpeedMultiplier = Mathf.Clamp01(1f - globalSpeedReduction);
        float finalSpeed = moveSpeed * speedMultiplier * globalSpeedMultiplier;
        RefreshMoveSpeedDebugFields(globalSpeedReduction, globalSpeedMultiplier, finalSpeed);
        transform.position = Vector3.MoveTowards(transform.position, target.position, finalSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) <= 0.05f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypointPath.Count)
            {
                ReachGoal();
            }
        }
    }

    private void UpdateFacingDirection()
    {
        if (!faceMovementDirection)
            return;

        CacheVisualComponents();

        if (spriteRenderer == null)
            return;

        float horizontalMovement = GetFacingHorizontalDirection();
        if (Mathf.Abs(horizontalMovement) < horizontalFacingThreshold)
            return;

        bool movingRight = horizontalMovement > 0f;
        spriteRenderer.flipX = spriteFacesRightByDefault ? !movingRight : movingRight;
    }

    private float GetFacingHorizontalDirection()
    {
        if (waypointPath != null && currentWaypointIndex < waypointPath.Count)
        {
            Transform target = waypointPath.GetWaypoint(currentWaypointIndex);
            if (target != null)
            {
                float targetDirectionX = target.position.x - transform.position.x;
                if (Mathf.Abs(targetDirectionX) >= horizontalFacingThreshold)
                    return targetDirectionX;
            }
        }

        return frameMovementDelta.x;
    }

    private void RefreshMoveSpeedDebugFields(float globalSpeedReduction, float globalSpeedMultiplier, float finalSpeed)
    {
        debugSpeedMultiplier = speedMultiplier;
        debugGlobalSpeedReduction = globalSpeedReduction;
        debugGlobalSpeedMultiplier = globalSpeedMultiplier;
        debugFinalMoveSpeed = finalSpeed;
    }

    private MonsterWalkAnimationSet GetWalkAnimationSetForWave(int wave)
    {
        EnsureWalkAnimationSets();

        if (walkAnimationSets == null || walkAnimationSets.Length == 0)
            return null;

        int safeWave = Mathf.Max(1, wave);

        foreach (MonsterWalkAnimationSet animationSet in walkAnimationSets)
        {
            if (animationSet == null)
                continue;

            int startWave = Mathf.Max(1, animationSet.startWave);
            int endWave = Mathf.Max(startWave, animationSet.endWave);

            if (safeWave >= startWave && safeWave <= endWave)
                return animationSet;
        }

        int fallbackIndex = Mathf.Clamp((safeWave - 1) / 10, 0, walkAnimationSets.Length - 1);
        return walkAnimationSets[fallbackIndex];
    }

    private void CacheVisualComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator != null)
            return;

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void EnsureWalkAnimationSets()
    {
        const int DefaultSetCount = 10;

        if (walkAnimationSets == null || walkAnimationSets.Length != DefaultSetCount)
        {
            MonsterWalkAnimationSet[] resizedSets = new MonsterWalkAnimationSet[DefaultSetCount];

            if (walkAnimationSets != null)
            {
                int copyCount = Mathf.Min(walkAnimationSets.Length, resizedSets.Length);
                for (int i = 0; i < copyCount; i++)
                    resizedSets[i] = walkAnimationSets[i];
            }

            walkAnimationSets = resizedSets;
        }

        for (int i = 0; i < walkAnimationSets.Length; i++)
        {
            if (walkAnimationSets[i] == null)
                walkAnimationSets[i] = new MonsterWalkAnimationSet();

            walkAnimationSets[i].startWave = (i * 10) + 1;
            walkAnimationSets[i].endWave = (i + 1) * 10;
        }
    }

    public void TakeDamage(double damage)
    {
        if (!IsAlive) return;

        double appliedDamage = Math.Max(0.0, damage);
        if (appliedDamage <= 0.0) return;

        currentHp -= appliedDamage;
        currentHp = Math.Max(0.0, currentHp);

        OnDamageTaken?.Invoke(this, appliedDamage);
        NotifyHpChanged();

        if (currentHp <= 0.0)
        {
            Die();
        }
    }

    public void AddDebuff(DebuffInstance debuff)
    {
        if (debuff == null) return;

        DebuffInstance existing = debuffs.Find(d => d.debuffType == debuff.debuffType && d.source == debuff.source);
        if (debuff.debuffType == DebuffType.CorruptionLord && existing != null)
            return;

        if (debuff.debuffType == DebuffType.Burn)
        {
            if (existing != null && existing.maxStack > 1)
            {
                existing.stack = Mathf.Clamp(existing.stack + 1, 1, existing.maxStack);
                existing.value = debuff.value;
                existing.duration = debuff.duration;
                existing.remainTime = debuff.duration;
                return;
            }
        }

        if (existing != null)
        {
            existing.value = debuff.value;
            existing.duration = debuff.duration;
            existing.remainTime = debuff.duration;
            existing.stack = Mathf.Max(1, debuff.stack);
            existing.maxStack = Mathf.Max(1, debuff.maxStack);
            existing.damageMultiplierOnExpire = debuff.damageMultiplierOnExpire;
            existing.currentHpDamagePercentOnExpire = debuff.currentHpDamagePercentOnExpire;
            existing.maxHpDamagePercentPerTick = debuff.maxHpDamagePercentPerTick;
            existing.tickInterval = debuff.tickInterval;
            existing.tickTimer = debuff.tickTimer;
            return;
        }

        debuff.remainTime = debuff.duration;
        if (debuff.tickInterval > 0f && debuff.tickTimer <= 0f)
            debuff.tickTimer = debuff.tickInterval;

        debuffs.Add(debuff);
    }

    public bool HasDebuff(DebuffType debuffType, UnitController source = null)
    {
        return debuffs.Exists(d => d.debuffType == debuffType && (source == null || d.source == source) && d.remainTime > 0f);
    }

    public float GetDebuffValue(DebuffType debuffType, UnitController source = null)
    {
        float value = 0f;

        foreach (DebuffInstance debuff in debuffs)
        {
            if (debuff.debuffType != debuffType) continue;
            if (source != null && debuff.source != source) continue;
            if (debuff.remainTime <= 0f) continue;

            value += debuff.value;
        }

        return value;
    }

    private void UpdateDebuffs()
    {
        speedMultiplier = 1f;
        isStunned = false;

        for (int i = debuffs.Count - 1; i >= 0; i--)
        {
            DebuffInstance d = debuffs[i];
            d.remainTime -= Time.deltaTime;

            switch (d.debuffType)
            {
                case DebuffType.Burn:
                    TakeDamage(d.value * Mathf.Max(1, d.stack) * Time.deltaTime);
                    break;

                case DebuffType.Slow:
                    speedMultiplier *= Mathf.Clamp01(1f - d.value);
                    break;

                case DebuffType.Stun:
                    isStunned = true;
                    break;

                case DebuffType.CorruptionLord:
                    TickMaxHpDamageDebuff(d);
                    break;

                case DebuffType.DamageTakenUp:
                case DebuffType.Silence:
                    break;
            }

            if (d.remainTime <= 0f)
            {
                ApplyDebuffExpireDamage(d);
                debuffs.RemoveAt(i);
            }
        }
    }

    private void ApplyDebuffExpireDamage(DebuffInstance debuff)
    {
        if (debuff == null || debuff.source == null || !IsAlive)
            return;

        if (debuff.currentHpDamagePercentOnExpire > 0f)
            DamageSystem.DealRawDamage(debuff.source, this, CurrentHp * debuff.currentHpDamagePercentOnExpire);

        if (debuff.damageMultiplierOnExpire > 0f && IsAlive)
            DamageSystem.DealDamage(debuff.source, this, debuff.source.CurrentAttackPower * debuff.damageMultiplierOnExpire);
    }

    private void TickMaxHpDamageDebuff(DebuffInstance debuff)
    {
        if (debuff == null || debuff.source == null || debuff.maxHpDamagePercentPerTick <= 0f || debuff.tickInterval <= 0f)
            return;

        debuff.tickTimer -= Time.deltaTime;
        while (debuff.tickTimer <= 0f && IsAlive)
        {
            DamageSystem.DealRawDamage(debuff.source, this, MaxHp * debuff.maxHpDamagePercentPerTick);
            debuff.tickTimer += debuff.tickInterval;
        }
    }

    public float GetHpPercent()
    {
        if (maxHp <= 0.0) return 0f;

        double percent = currentHp / maxHp;
        return Mathf.Clamp01((float)percent);
    }

    public float GetDamageTakenMultiplier()
    {
        return 1f + Mathf.Max(0f, GetDebuffValue(DebuffType.DamageTakenUp));
    }

    private void ReachGoal()
    {
        if (IsBountyElite)
        {
            NotifyBountyEliteRemoved();
            gameObject.SetActive(false);

            if (destroyOnGoal)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (waveManager != null)
        {
            waveManager.NotifyMonsterReachedGoal();
        }

        gameObject.SetActive(false);

        if (destroyOnGoal)
        {
            Destroy(gameObject);
        }
    }

    private void Die()
    {
        if (IsBountyElite)
        {
            NotifyBountyEliteKilled();
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        GoldManager goldManager = FindAnyObjectByType<GoldManager>();
        if (goldManager != null && rewardGold > 0)
        {
            goldManager.AddGold(rewardGold);
        }

        if (waveManager != null)
        {
            waveManager.NotifyMonsterDead();
        }

        if (isBoss)
        {
            if (waveManager != null && waveManager.gameEnded)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            BossRewardController bossRewardController = FindAnyObjectByType<BossRewardController>();
            if (bossRewardController != null)
            {
                bossRewardController.OpenBossAuction();
            }
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void NotifyBountyEliteKilled()
    {
        if (bountyManager == null)
            bountyManager = BountyManager.Instance != null ? BountyManager.Instance : FindAnyObjectByType<BountyManager>();

        if (bountyManager != null)
            bountyManager.OnBountyEliteKilled(bountyDifficulty);
    }

    private void NotifyBountyEliteRemoved()
    {
        if (bountyManager == null)
            bountyManager = BountyManager.Instance != null ? BountyManager.Instance : FindAnyObjectByType<BountyManager>();

        if (bountyManager != null)
            bountyManager.OnBountyEliteRemoved(this);
    }

    private void NotifyHpChanged()
    {
        OnHpChanged?.Invoke(this, currentHp, maxHp);
    }
}
