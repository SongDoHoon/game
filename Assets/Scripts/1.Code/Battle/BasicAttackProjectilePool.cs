using System.Collections.Generic;
using UnityEngine;

public class BasicAttackProjectilePool : MonoBehaviour
{
    private const int InitialPoolSize = 30;

    private static BasicAttackProjectilePool instance;

    private readonly Queue<BasicAttackProjectile> availableProjectiles = new();
    private readonly HashSet<BasicAttackProjectile> pooledProjectiles = new();

    public static BasicAttackProjectilePool Instance
    {
        get
        {
            if (instance != null)
                return instance;

            GameObject poolObject = new GameObject("BasicAttackProjectilePool");
            instance = poolObject.AddComponent<BasicAttackProjectilePool>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Prewarm();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        availableProjectiles.Clear();
        pooledProjectiles.Clear();
    }

    public BasicAttackProjectile Get(
        string projectileName,
        Vector3 position,
        Vector3 scale,
        Color projectileColor,
        float projectileSize)
    {
        BasicAttackProjectile projectile = availableProjectiles.Count > 0
            ? availableProjectiles.Dequeue()
            : CreateProjectile();

        if (projectile == null)
            projectile = CreateProjectile();

        projectile.name = projectileName;
        projectile.transform.SetParent(null);
        projectile.PrepareFromPool(position, scale, projectileColor, projectileSize);
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    public void Release(BasicAttackProjectile projectile)
    {
        if (projectile == null)
            return;

        if (!pooledProjectiles.Contains(projectile))
        {
            Destroy(projectile.gameObject);
            return;
        }

        projectile.CleanupForPool();
        projectile.transform.SetParent(transform);
        projectile.gameObject.SetActive(false);
        availableProjectiles.Enqueue(projectile);
    }

    public void Clear()
    {
        foreach (BasicAttackProjectile projectile in pooledProjectiles)
        {
            if (projectile != null)
                Destroy(projectile.gameObject);
        }

        availableProjectiles.Clear();
        pooledProjectiles.Clear();
    }

    private void Prewarm()
    {
        if (pooledProjectiles.Count > 0)
            return;

        for (int i = 0; i < InitialPoolSize; i++)
        {
            BasicAttackProjectile projectile = CreateProjectile();
            projectile.CleanupForPool();
            projectile.transform.SetParent(transform);
            projectile.gameObject.SetActive(false);
            availableProjectiles.Enqueue(projectile);
        }
    }

    private BasicAttackProjectile CreateProjectile()
    {
        BasicAttackProjectile projectile = BasicAttackProjectile.CreatePooledProjectile();
        pooledProjectiles.Add(projectile);
        return projectile;
    }
}
