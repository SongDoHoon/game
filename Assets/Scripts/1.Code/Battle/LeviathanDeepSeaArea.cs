using System.Collections.Generic;
using UnityEngine;

public class LeviathanDeepSeaArea : MonoBehaviour
{
    private const int CircleTextureSize = 64;
    private const float TickInterval = 1f;
    private const float DebuffRefreshDuration = 1.1f;

    private static readonly List<LeviathanDeepSeaArea> activeAreas = new();
    private static Sprite circleSprite;

    private UnitController sourceUnit;
    private SkillData skill;
    private SpriteRenderer spriteRenderer;
    private float radius;
    private float remainTime;
    private float tickTimer;
    private bool isExploding;
    private float explosionRemainTime;

    public static bool HasActiveArea()
    {
        RemoveNullAreas();
        return activeAreas.Count > 0;
    }

    public static void TryCreate(UnitController source, SkillData skillData, Vector3 center)
    {
        if (source == null || skillData == null || !skillData.createDeepSeaAreaOnBasicAttackImpact)
            return;

        RemoveNullAreas();

        float areaRadius = Mathf.Max(0.01f, skillData.deepSeaAreaRadius);
        if (HasOverlappingArea(center, areaRadius))
            return;

        int maxCount = Mathf.Max(1, skillData.maxDeepSeaAreaCount);
        while (activeAreas.Count >= maxCount)
            DestroyOldestArea();

        GameObject areaObject = new GameObject("LeviathanDeepSeaArea");
        areaObject.transform.position = center;
        areaObject.transform.localScale = Vector3.one * areaRadius * 2f;

        LeviathanDeepSeaArea area = areaObject.AddComponent<LeviathanDeepSeaArea>();
        area.Initialize(source, skillData, areaRadius);
    }

    public static bool TryStartExplosionOnActiveAreas(UnitController source, SkillData skillData)
    {
        if (source == null || skillData == null || !skillData.activeExplodesDeepSeaAreas)
            return false;

        RemoveNullAreas();

        bool activatedAny = false;
        foreach (LeviathanDeepSeaArea area in activeAreas)
        {
            if (area == null)
                continue;

            area.StartExplosion(source, skillData);
            activatedAny = true;
        }

        return activatedAny;
    }

    private static bool HasOverlappingArea(Vector3 center, float areaRadius)
    {
        foreach (LeviathanDeepSeaArea area in activeAreas)
        {
            if (area == null)
                continue;

            float combinedRadius = area.radius + areaRadius;
            if (Vector3.Distance(center, area.transform.position) < combinedRadius)
                return true;
        }

        return false;
    }

    private static void DestroyOldestArea()
    {
        RemoveNullAreas();
        if (activeAreas.Count <= 0)
            return;

        LeviathanDeepSeaArea oldestArea = activeAreas[0];
        activeAreas.RemoveAt(0);

        if (oldestArea != null)
            Destroy(oldestArea.gameObject);
    }

    private static void RemoveNullAreas()
    {
        for (int i = activeAreas.Count - 1; i >= 0; i--)
        {
            if (activeAreas[i] == null)
                activeAreas.RemoveAt(i);
        }
    }

    private void Initialize(UnitController source, SkillData skillData, float areaRadius)
    {
        sourceUnit = source;
        skill = skillData;
        radius = areaRadius;
        remainTime = Mathf.Max(0.01f, skillData.deepSeaAreaDuration);
        tickTimer = 0f;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetCircleSprite();
        spriteRenderer.color = GetVisibleColor(skillData.deepSeaAreaColor);
        spriteRenderer.sortingOrder = 18;

        activeAreas.Add(this);
    }

    private void Update()
    {
        remainTime -= Time.deltaTime;
        if (isExploding)
            explosionRemainTime -= Time.deltaTime;

        if (remainTime <= 0f && (!isExploding || explosionRemainTime <= 0f))
        {
            Destroy(gameObject);
            return;
        }

        if (isExploding && explosionRemainTime <= 0f)
        {
            isExploding = false;
            RefreshVisual();
        }

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = TickInterval;
        ApplyAreaTick();
    }

    private void OnDestroy()
    {
        activeAreas.Remove(this);
    }

    private void StartExplosion(UnitController newSource, SkillData newSkill)
    {
        sourceUnit = newSource;
        skill = newSkill;
        isExploding = true;
        explosionRemainTime = Mathf.Max(0.01f, newSkill.deepSeaExplosionDuration);
        remainTime = Mathf.Max(remainTime, explosionRemainTime);
        tickTimer = 0f;
        RefreshVisual();
    }

    private void ApplyAreaTick()
    {
        if (sourceUnit == null || skill == null)
            return;

        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsInactive.Exclude);
        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (Vector3.Distance(transform.position, monster.transform.position) > radius)
                continue;

            ApplySlow(monster, GetCurrentSlow());
            DamageSystem.DealDamage(sourceUnit, monster, sourceUnit.CurrentAttackPower * GetCurrentDamageMultiplier());
        }
    }

    private float GetCurrentSlow()
    {
        if (isExploding)
            return Mathf.Max(0f, skill.deepSeaExplosionSlow);

        return Mathf.Max(0f, skill.deepSeaAreaSlow);
    }

    private float GetCurrentDamageMultiplier()
    {
        if (isExploding)
            return Mathf.Max(0f, skill.deepSeaExplosionDamageMultiplierPerSecond);

        return Mathf.Max(0f, skill.deepSeaAreaDamageMultiplierPerSecond);
    }

    private void ApplySlow(MonsterController monster, float value)
    {
        if (monster == null || value <= 0f)
            return;

        monster.AddDebuff(new DebuffInstance
        {
            debuffType = DebuffType.Slow,
            value = value,
            duration = DebuffRefreshDuration,
            remainTime = DebuffRefreshDuration,
            stack = 1,
            maxStack = 1,
            source = sourceUnit
        });
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null || skill == null)
            return;

        spriteRenderer.color = GetVisibleColor(isExploding ? skill.deepSeaExplosionColor : skill.deepSeaAreaColor);
    }

    private static Color GetVisibleColor(Color color)
    {
        if (color.a < 0.25f)
            color.a = 0.25f;

        return color;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        Texture2D texture = new Texture2D(CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (CircleTextureSize - 1) * 0.5f;
        float radius = center;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < CircleTextureSize; y++)
        {
            for (int x = 0; x < CircleTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
            new Vector2(0.5f, 0.5f),
            CircleTextureSize);

        return circleSprite;
    }
}
