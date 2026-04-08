using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public event Action<MonsterController, float> OnDamageTaken;
    public event Action<MonsterController, float, float> OnHpChanged;

    [Header("Info")]
    public MonsterType monsterType = MonsterType.Normal;

    [Header("Stats")]
    public float maxHp = 100f;
    public float currentHp = 100f;
    public float moveSpeed = 2f;
    public int rewardGold = 10;
    public bool isBoss = false;

    [Header("Path")]
    public WaypointPath waypointPath;
    public bool destroyOnGoal = true;

    private int currentWaypointIndex;
    private readonly List<DebuffInstance> debuffs = new();
    private float speedMultiplier = 1f;
    private bool isStunned;
    private WaveManager waveManager;

    public bool IsAlive => currentHp > 0f;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    private void Start()
    {
        currentHp = maxHp;
        NotifyHpChanged();

        if (waypointPath != null)
        {
            InitializePath();
        }
    }

    private void Update()
    {
        if (!IsAlive) return;

        UpdateDebuffs();
        MoveAlongPath();
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

        float finalSpeed = moveSpeed * speedMultiplier;
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

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        float appliedDamage = Mathf.Max(0f, damage);
        if (appliedDamage <= 0f) return;

        currentHp -= appliedDamage;
        currentHp = Mathf.Max(0f, currentHp);

        OnDamageTaken?.Invoke(this, appliedDamage);
        NotifyHpChanged();

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    public void AddDebuff(DebuffInstance debuff)
    {
        if (debuff == null) return;

        if (debuff.debuffType == DebuffType.Burn)
        {
            DebuffInstance existing = debuffs.Find(d => d.debuffType == DebuffType.Burn && d.source == debuff.source);

            if (existing != null && existing.maxStack > 1)
            {
                existing.stack = Mathf.Clamp(existing.stack + 1, 1, existing.maxStack);
                existing.value = debuff.value;
                existing.remainTime = debuff.duration;
                return;
            }
        }

        debuff.remainTime = debuff.duration;
        debuffs.Add(debuff);
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
            }

            if (d.remainTime <= 0f)
            {
                debuffs.RemoveAt(i);
            }
        }
    }

    public float GetHpPercent()
    {
        if (maxHp <= 0f) return 0f;
        return currentHp / maxHp;
    }

    private void ReachGoal()
    {
        gameObject.SetActive(false);

        if (destroyOnGoal)
        {
            Destroy(gameObject);
        }
    }

    private void Die()
    {
        GoldManager goldManager = FindFirstObjectByType<GoldManager>();
        if (goldManager != null)
        {
            goldManager.AddGold(rewardGold);
        }

        if (waveManager != null)
        {
            waveManager.NotifyMonsterDead();
        }

        if (isBoss)
        {
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
