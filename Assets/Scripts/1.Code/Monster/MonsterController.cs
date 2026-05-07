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

    [Header("Path")]
    public WaypointPath waypointPath;
    public bool destroyOnGoal = true;

    [Header("Animation")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public MonsterWalkAnimationSet[] walkAnimationSets = new MonsterWalkAnimationSet[10];

    [Header("Facing")]
    public bool faceMovementDirection = true;
    public bool spriteFacesRightByDefault = true;
    public float horizontalFacingThreshold = 0.001f;

    private int currentWaypointIndex;
    private readonly List<DebuffInstance> debuffs = new();
    private float speedMultiplier = 1f;
    private bool isStunned;
    private WaveManager waveManager;
    private Vector3 lastPosition;
    private Vector3 frameMovementDelta;

    public bool IsAlive => currentHp > 0.0;
    public double CurrentHp => currentHp;
    public double MaxHp => maxHp;

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

        Transform target = waypointPath.GetWaypoint(currentWaypointIndex);
        if (target == null) return;

        float auctionSpeedMultiplier = Mathf.Clamp01(1f - GameModifierState.MonsterMoveSpeedReduction);
        float finalSpeed = moveSpeed * speedMultiplier * auctionSpeedMultiplier;
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

        float horizontalMovement = frameMovementDelta.x;
        if (Mathf.Abs(horizontalMovement) < horizontalFacingThreshold)
            return;

        bool movingRight = horizontalMovement > 0f;
        spriteRenderer.flipX = spriteFacesRightByDefault ? !movingRight : movingRight;
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
            return;
        }

        debuff.remainTime = debuff.duration;
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

                case DebuffType.DamageTakenUp:
                case DebuffType.Silence:
                    break;
            }

            if (d.remainTime <= 0f)
            {
                debuffs.RemoveAt(i);
            }
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
        GoldManager goldManager = FindFirstObjectByType<GoldManager>();
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

            BossRewardController bossRewardController = FindFirstObjectByType<BossRewardController>();
            if (bossRewardController != null)
            {
                bossRewardController.OpenBossAuction();
            }
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void NotifyHpChanged()
    {
        OnHpChanged?.Invoke(this, currentHp, maxHp);
    }
}
