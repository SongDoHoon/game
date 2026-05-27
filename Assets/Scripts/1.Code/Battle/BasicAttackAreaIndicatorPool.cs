using System.Collections.Generic;
using UnityEngine;

public class BasicAttackAreaIndicatorPool : MonoBehaviour
{
    private const int InitialPoolSize = 10;
    private const int SortingOrder = 20;

    private static BasicAttackAreaIndicatorPool instance;

    private readonly Queue<BasicAttackAreaIndicator> availableIndicators = new();
    private readonly HashSet<BasicAttackAreaIndicator> pooledIndicators = new();

    public static BasicAttackAreaIndicatorPool Instance
    {
        get
        {
            if (instance != null)
                return instance;

            GameObject poolObject = new GameObject("BasicAttackAreaIndicatorPool");
            instance = poolObject.AddComponent<BasicAttackAreaIndicatorPool>();
            return instance;
        }
    }

    public static void ReleaseIndicator(GameObject indicatorObject)
    {
        if (indicatorObject == null)
            return;

        if (instance == null)
        {
            Destroy(indicatorObject);
            return;
        }

        instance.Release(indicatorObject);
    }

    public static void ClearPool()
    {
        if (instance != null)
            instance.Clear();
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

        availableIndicators.Clear();
        pooledIndicators.Clear();
    }

    public GameObject Get(
        Vector3 position,
        Vector3 scale,
        Sprite sprite,
        Color color,
        float duration,
        bool autoRelease)
    {
        BasicAttackAreaIndicator indicator = availableIndicators.Count > 0
            ? availableIndicators.Dequeue()
            : CreateIndicator();

        if (indicator == null)
            indicator = CreateIndicator();

        indicator.name = "BasicAttackAreaIndicator";
        indicator.transform.SetParent(null);
        indicator.Prepare(position, scale, sprite, color, SortingOrder);
        indicator.gameObject.SetActive(true);

        if (autoRelease)
            indicator.StartAutoRelease(duration);

        return indicator.gameObject;
    }

    public void Release(GameObject indicatorObject)
    {
        if (indicatorObject == null)
            return;

        BasicAttackAreaIndicator indicator = indicatorObject.GetComponent<BasicAttackAreaIndicator>();
        if (indicator == null)
        {
            Destroy(indicatorObject);
            return;
        }

        Release(indicator);
    }

    public void Release(BasicAttackAreaIndicator indicator)
    {
        if (indicator == null)
            return;

        if (!pooledIndicators.Contains(indicator))
        {
            Destroy(indicator.gameObject);
            return;
        }

        if (!indicator.TryMarkReleased())
            return;

        indicator.CleanupForPool();
        indicator.transform.SetParent(transform);
        indicator.gameObject.SetActive(false);
        availableIndicators.Enqueue(indicator);
    }

    public void Clear()
    {
        foreach (BasicAttackAreaIndicator indicator in pooledIndicators)
        {
            if (indicator != null)
                Destroy(indicator.gameObject);
        }

        availableIndicators.Clear();
        pooledIndicators.Clear();
    }

    private void Prewarm()
    {
        if (pooledIndicators.Count > 0)
            return;

        for (int i = 0; i < InitialPoolSize; i++)
        {
            BasicAttackAreaIndicator indicator = CreateIndicator();
            indicator.CleanupForPool();
            indicator.transform.SetParent(transform);
            indicator.gameObject.SetActive(false);
            availableIndicators.Enqueue(indicator);
        }
    }

    private BasicAttackAreaIndicator CreateIndicator()
    {
        GameObject indicatorObject = new GameObject("BasicAttackAreaIndicator");

        SpriteRenderer spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = SortingOrder;

        BasicAttackAreaIndicator indicator = indicatorObject.AddComponent<BasicAttackAreaIndicator>();
        indicator.Initialize(this);
        pooledIndicators.Add(indicator);
        return indicator;
    }
}
