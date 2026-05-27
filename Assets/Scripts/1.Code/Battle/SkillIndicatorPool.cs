using System.Collections.Generic;
using UnityEngine;

public class SkillIndicatorPool : MonoBehaviour
{
    private const int InitialLinePoolSize = 8;
    private const int InitialConePoolSize = 4;
    private const int LineSortingOrder = 20;
    private const int ConeSortingOrder = 21;

    private static SkillIndicatorPool instance;
    private static Material sharedConeMaterial;

    private readonly Queue<SkillIndicator> availableLineIndicators = new();
    private readonly Queue<SkillIndicator> availableConeIndicators = new();
    private readonly HashSet<SkillIndicator> pooledIndicators = new();

    public static SkillIndicatorPool Instance
    {
        get
        {
            if (instance != null)
                return instance;

            GameObject poolObject = new GameObject("SkillIndicatorPool");
            instance = poolObject.AddComponent<SkillIndicatorPool>();
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
        {
            instance = null;

            if (sharedConeMaterial != null)
            {
                Destroy(sharedConeMaterial);
                sharedConeMaterial = null;
            }
        }

        availableLineIndicators.Clear();
        availableConeIndicators.Clear();
        pooledIndicators.Clear();
    }

    public static void ClearPool()
    {
        if (instance != null)
            instance.Clear();
    }

    public void ShowLine(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Sprite sprite,
        Color color,
        float duration)
    {
        SkillIndicator indicator = GetLineIndicator();
        indicator.name = "HorizontalLineSkillIndicator";
        indicator.transform.SetParent(null);
        indicator.PrepareLine(position, rotation, scale, sprite, color, LineSortingOrder);
        indicator.gameObject.SetActive(true);
        indicator.StartAutoRelease(duration);
    }

    public void ShowCone(
        Vector3 position,
        Vector3 forward,
        float range,
        float halfAngle,
        Color color,
        float duration,
        int segmentCount)
    {
        Material material = GetSharedConeMaterial();
        SkillIndicator indicator = GetConeIndicator();
        indicator.name = "ConeSkillIndicator";
        indicator.transform.SetParent(null);
        indicator.PrepareCone(position, forward, range, halfAngle, color, ConeSortingOrder, segmentCount, material);
        indicator.gameObject.SetActive(true);
        indicator.StartAutoRelease(duration);
    }

    public void Release(SkillIndicator indicator)
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

        bool isConeIndicator = indicator.GetComponent<MeshRenderer>() != null;
        indicator.CleanupForPool();
        indicator.transform.SetParent(transform);
        indicator.gameObject.SetActive(false);

        if (isConeIndicator)
            availableConeIndicators.Enqueue(indicator);
        else
            availableLineIndicators.Enqueue(indicator);
    }

    public void Clear()
    {
        foreach (SkillIndicator indicator in pooledIndicators)
        {
            if (indicator != null)
                Destroy(indicator.gameObject);
        }

        availableLineIndicators.Clear();
        availableConeIndicators.Clear();
        pooledIndicators.Clear();
    }

    private void Prewarm()
    {
        if (pooledIndicators.Count > 0)
            return;

        for (int i = 0; i < InitialLinePoolSize; i++)
        {
            SkillIndicator indicator = CreateLineIndicator();
            indicator.CleanupForPool();
            indicator.transform.SetParent(transform);
            indicator.gameObject.SetActive(false);
            availableLineIndicators.Enqueue(indicator);
        }

        for (int i = 0; i < InitialConePoolSize; i++)
        {
            SkillIndicator indicator = CreateConeIndicator();
            indicator.CleanupForPool();
            indicator.transform.SetParent(transform);
            indicator.gameObject.SetActive(false);
            availableConeIndicators.Enqueue(indicator);
        }
    }

    private SkillIndicator GetLineIndicator()
    {
        SkillIndicator indicator = availableLineIndicators.Count > 0
            ? availableLineIndicators.Dequeue()
            : CreateLineIndicator();

        return indicator != null ? indicator : CreateLineIndicator();
    }

    private SkillIndicator GetConeIndicator()
    {
        SkillIndicator indicator = availableConeIndicators.Count > 0
            ? availableConeIndicators.Dequeue()
            : CreateConeIndicator();

        return indicator != null ? indicator : CreateConeIndicator();
    }

    private SkillIndicator CreateLineIndicator()
    {
        GameObject indicatorObject = new GameObject("HorizontalLineSkillIndicator");
        indicatorObject.AddComponent<SpriteRenderer>();

        SkillIndicator indicator = indicatorObject.AddComponent<SkillIndicator>();
        indicator.Initialize(this);
        pooledIndicators.Add(indicator);
        return indicator;
    }

    private SkillIndicator CreateConeIndicator()
    {
        GameObject indicatorObject = new GameObject("ConeSkillIndicator");
        indicatorObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = indicatorObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetSharedConeMaterial();
        meshRenderer.sortingOrder = ConeSortingOrder;

        SkillIndicator indicator = indicatorObject.AddComponent<SkillIndicator>();
        indicator.Initialize(this);
        pooledIndicators.Add(indicator);
        return indicator;
    }

    private static Material GetSharedConeMaterial()
    {
        if (sharedConeMaterial != null)
            return sharedConeMaterial;

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader == null)
            return null;

        sharedConeMaterial = new Material(spriteShader)
        {
            name = "SkillIndicatorSharedMaterial"
        };

        return sharedConeMaterial;
    }
}
